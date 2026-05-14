using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class OtpVerificationService(
    AppDbContext db,
    IConfiguration config,
    IWebHostEnvironment env,
    IOtpDeliveryService delivery) : IOtpVerificationService
{
    private string Pepper =>
        config["Otp:Pepper"] ?? "sendpay-otp-pepper-change-in-production-min-length-32!!";

    public Task<VerificationStartResult> StartTopUpAsync(int userId, decimal amount) =>
        CreateAsync(userId, OtpPayloadBuilder.TopUp(amount), "Nạp tiền vào ví");

    public Task<VerificationStartResult> StartTransferAsync(
        int userId, string receiverPhone, decimal amount, string? note) =>
        CreateAsync(userId, OtpPayloadBuilder.Transfer(receiverPhone, amount, note), "Chuyển tiền");

    public async Task VerifyTopUpAsync(int userId, Guid verificationId, string code, decimal amount) =>
        await VerifyAsync(userId, verificationId, code, OtpPayloadBuilder.TopUp(amount));

    public async Task VerifyTransferAsync(
        int userId, Guid verificationId, string code, string receiverPhone, decimal amount, string? note) =>
        await VerifyAsync(userId, verificationId, code,
            OtpPayloadBuilder.Transfer(receiverPhone, amount, note));

    private async Task<VerificationStartResult> CreateAsync(int userId, string payloadJson, string actionDescription)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.Phone })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        var code = ResolveOtpCodeToIssue();
        var id = Guid.NewGuid();
        var hash = HashOtp(id, code);

        var entity = new OtpChallenge
        {
            Id          = id,
            UserId      = userId,
            PayloadJson = payloadJson,
            CodeHash    = hash,
            ExpiresAt   = DateTime.UtcNow.AddMinutes(5)
        };
        db.OtpChallenges.Add(entity);
        await db.SaveChangesAsync();

        try
        {
            var notify     = await delivery.NotifyAsync(user.Email, user.Phone, code, actionDescription);
            var hintMsg    = notify.ProductionMessage;
            var showPlain  = env.IsDevelopment() || config.GetValue("Otp:Simulation", false);
            return new VerificationStartResult(id, 300, showPlain ? code : null, hintMsg);
        }
        catch
        {
            db.OtpChallenges.Remove(entity);
            await db.SaveChangesAsync();
            throw;
        }
    }

    private async Task VerifyAsync(int userId, Guid verificationId, string code, string expectedPayloadJson)
    {
        code = NormalizeOtpCodeForVerify(code);
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
            throw new InvalidOperationException("Mã OTP phải gồm đúng 6 chữ số.");

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var row = await db.OtpChallenges
                .Where(x => x.Id == verificationId)
                .FirstOrDefaultAsync();

            if (row is null)
                throw new InvalidOperationException("Phiên xác thực không tồn tại.");
            if (row.UserId != userId)
                throw new InvalidOperationException("Phiên xác thực không hợp lệ.");
            if (row.ConsumedAt.HasValue)
                throw new InvalidOperationException("Mã OTP đã được sử dụng.");
            if (row.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Mã OTP đã hết hạn. Vui lòng lấy mã mới.");
            if (row.PayloadJson != expectedPayloadJson)
                throw new InvalidOperationException(
                    "Nội dung giao dịch đã thay đổi so với lúc gửi OTP. Vui lòng lấy mã mới.");

            var expectedHash = HashOtp(row.Id, code);
            if (!FixedTimeHexEquals(row.CodeHash, expectedHash))
                throw new InvalidOperationException("Mã OTP không đúng.");

            row.ConsumedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static bool FixedTimeHexEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(a), Convert.FromHexString(b));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string HashOtp(Guid challengeId, string code) =>
        Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes($"{challengeId:N}:{code}:{Pepper}")))
            .ToLowerInvariant();

    string ResolveOtpCodeToIssue()
    {
        if (!config.GetValue("Otp:Simulation", false))
            return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

        var raw = config["Otp:SimulationFixedCode"];
        if (string.IsNullOrWhiteSpace(raw))
            return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

        var t = raw.Trim();
        if (t.Length is < 1 or > 6 || !t.All(char.IsDigit))
        {
            return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        }

        return t.PadLeft(6, '0');
    }

    string NormalizeOtpCodeForVerify(string? code)
    {
        if (!config.GetValue("Otp:Simulation", false))
            return code ?? "";

        var t = (code ?? "").Trim();
        if (t.Length is >= 1 and <= 6 && t.All(char.IsDigit))
            return t.PadLeft(6, '0');
        return t;
    }
}
