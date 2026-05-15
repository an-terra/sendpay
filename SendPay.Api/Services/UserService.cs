using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.User;
using SendPay.Api.Infrastructure;
using SendPay.Api.Models;
using SendPay.Api.Security;

namespace SendPay.Api.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<UserProfileResponse> GetProfileAsync(int userId)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw AppError.NotFound(ErrorCodes.UserNotFound, "Người dùng không tồn tại.");
        return new UserProfileResponse(u.Id, u.FullName, u.Email, u.Phone, u.Balance, u.CreatedAt,
            u.JapanBankName, u.JapanBankTopUpUrl);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw AppError.NotFound(ErrorCodes.UserNotFound, "Người dùng không tồn tại.");

        if (await db.Users.AnyAsync(x => x.Email == req.Email && x.Id != userId))
            throw AppError.BadRequest(ErrorCodes.EmailInUse, "Email đã được sử dụng.");

        if (await db.Users.AnyAsync(x => x.Phone == req.Phone && x.Id != userId))
            throw AppError.BadRequest(ErrorCodes.PhoneInUse, "Số điện thoại đã được sử dụng.");

        u.FullName = req.FullName.Trim();
        u.Email    = req.Email.Trim();
        u.Phone    = req.Phone.Trim();

        u.JapanBankName = string.IsNullOrWhiteSpace(req.JapanBankName)
            ? null
            : req.JapanBankName.Trim();

        if (string.IsNullOrWhiteSpace(req.JapanBankTopUpUrl))
            u.JapanBankTopUpUrl = null;
        else
        {
            var url = req.JapanBankTopUpUrl.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw AppError.BadRequest(ErrorCodes.ProfileUrlInvalid,
                    "URL ngân hàng phải bắt đầu bằng http:// hoặc https://.");
            u.JapanBankTopUpUrl = url;
        }

        await db.SaveChangesAsync();
        return new UserProfileResponse(u.Id, u.FullName, u.Email, u.Phone, u.Balance, u.CreatedAt,
            u.JapanBankName, u.JapanBankTopUpUrl);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw AppError.NotFound(ErrorCodes.UserNotFound, "Người dùng không tồn tại.");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, u.PasswordHash))
            throw AppError.Unauthorized(ErrorCodes.CurrentPasswordWrong, "Mật khẩu hiện tại không đúng.");

        PasswordPolicy.EnsureStrongOrThrow(req.NewPassword);

        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
    }

    public async Task<ReceiverLookupResponse> LookupTransferCounterpartyAsync(
        int userId, string? phone, string? accountNumber, string? bankName)
    {
        var phoneNorm = OtpPayloadBuilder.NormalizePhone(phone);
        var acctNorm  = OtpPayloadBuilder.NormalizeAccountKey(accountNumber);

        var mePhone = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Phone)
            .FirstOrDefaultAsync();

        if (mePhone is null)
            throw AppError.NotFound(ErrorCodes.UserNotFound, "Người dùng không tồn tại.");

        var meNorm = OtpPayloadBuilder.NormalizePhone(mePhone);

        if (string.IsNullOrEmpty(phoneNorm) && acctNorm.Length < 6)
            return NotFoundResponse();

        var saved = await db.Recipients.AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();

        Recipient? savedMatch = null;
        if (acctNorm.Length >= 6)
        {
            savedMatch = saved.FirstOrDefault(r =>
                OtpPayloadBuilder.NormalizeAccountKey(r.AccountNumber) == acctNorm
                && BankMatches(r.BankName, bankName));
        }

        if (savedMatch is null && !string.IsNullOrEmpty(phoneNorm))
        {
            savedMatch = saved.FirstOrDefault(r =>
                !string.IsNullOrEmpty(r.Phone) &&
                (r.Phone == phone || OtpPayloadBuilder.NormalizePhone(r.Phone) == phoneNorm));
        }

        if (savedMatch is not null)
        {
            var sp = OtpPayloadBuilder.NormalizePhone(savedMatch.Phone);
            if (!string.IsNullOrEmpty(sp) && sp == meNorm)
                return new ReceiverLookupResponse(false, null, true, savedMatch.Id, null, ToBankDisplay(savedMatch), null);

            var display = DisplayNameForSaved(savedMatch);
            var rph     = string.IsNullOrWhiteSpace(savedMatch.Phone) ? null : savedMatch.Phone.Trim();
            return new ReceiverLookupResponse(true, display, false, savedMatch.Id, "saved", ToBankDisplay(savedMatch), rph);
        }

        if (string.IsNullOrEmpty(phoneNorm))
            return NotFoundResponse();

        if (meNorm == phoneNorm)
            return new ReceiverLookupResponse(false, null, true, null, null, null, null);

        var phoneRaw = phone ?? "";
        var row = await db.Users.AsNoTracking()
            .Where(u => u.Phone == phoneRaw
                     || u.Phone == phoneNorm
                     || u.Phone.Replace(" ", "")
                               .Replace("-", "")
                               .Replace("+", "")
                               .Replace("(", "")
                               .Replace(")", "")
                               .Replace(".", "") == phoneNorm)
            .Select(u => new { u.FullName, u.Phone })
            .FirstOrDefaultAsync();

        if (row is null)
            return NotFoundResponse();

        return new ReceiverLookupResponse(true, row.FullName, false, null, "registered", null, row.Phone);
    }

    private static ReceiverLookupResponse NotFoundResponse() =>
        new(false, null, false, null, null, null, null);

    private static bool BankMatches(string? recipientBank, string? queryBank)
    {
        if (string.IsNullOrWhiteSpace(queryBank)) return true;
        if (string.IsNullOrWhiteSpace(recipientBank)) return false;
        return recipientBank.Contains(queryBank.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayNameForSaved(Recipient r) =>
        string.IsNullOrWhiteSpace(r.AccountHolderName)
            ? r.Name
            : $"{r.Name} ({r.AccountHolderName})";

    private static string? ToBankDisplay(Recipient r)
    {
        var acct = OtpPayloadBuilder.NormalizeAccountKey(r.AccountNumber);
        if (string.IsNullOrEmpty(acct))
            return string.IsNullOrWhiteSpace(r.BankName) ? null : r.BankName;
        var tail = acct.Length <= 4 ? acct : acct[^4..];
        var bank = string.IsNullOrWhiteSpace(r.BankName) ? "STK" : r.BankName!;
        return $"{bank} · ****{tail}";
    }
}
