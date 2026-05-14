using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Wallet;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class WalletService(
    AppDbContext db,
    IWebHostEnvironment env,
    IConfiguration config) : IWalletService
{
    public async Task<WalletResponse> GetBalanceAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        return ToResponse(user);
    }

    public async Task<WalletTopUpResponse> TopUpAsync(int userId, TopUpRequest req)
    {
        if (!env.IsDevelopment() && !config.GetValue("Features:AllowDemoTopUp", false))
            throw new InvalidOperationException(
                "Nạp tiền qua API đã tắt trên production. Hãy bật Features:AllowDemoTopUp (chỉ demo) hoặc tích hợp cổng thanh toán thật.");

        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        var instant = config.GetValue("Features:InstantWalletTopUp", false);
        if (instant)
        {
            user.Balance += req.Amount;
            db.Transactions.Add(new Transaction
            {
                SenderId = userId,
                ReceiverId = userId,
                Amount = req.Amount,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Note = "Nạp tiền vào ví (instant)"
            });
            await db.SaveChangesAsync();
            return new WalletTopUpResponse
            {
                Mode = "instant",
                Wallet = ToResponse(user),
                ExpectedAmount = req.Amount
            };
        }

        var hours = config.GetValue("Features:TopUpIntentExpiryHours", 72);
        var code = await GenerateUniqueReferenceAsync(userId);
        var intent = new TopUpIntent
        {
            UserId = user.Id,
            ExpectedAmount = req.Amount,
            ReferenceCode = code,
            Status = TopUpIntentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(hours),
        };
        db.TopUpIntents.Add(intent);
        await db.SaveChangesAsync();

        return new WalletTopUpResponse
        {
            Mode = "bank_pending",
            Wallet = ToResponse(user),
            IntentId = intent.Id,
            ReferenceCode = intent.ReferenceCode,
            ExpiresAt = intent.ExpiresAt,
            ExpectedAmount = intent.ExpectedAmount,
        };
    }

    async Task<string> GenerateUniqueReferenceAsync(int userId)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var code = $"SPU{userId}-{Guid.NewGuid():N}";
            if (!await db.TopUpIntents.AnyAsync(x => x.ReferenceCode == code))
                return code;
        }

        throw new InvalidOperationException("Không tạo được mã tham chiếu duy nhất.");
    }

    private static WalletResponse ToResponse(User u) => new()
    {
        FullName = u.FullName,
        Phone = u.Phone,
        Balance = u.Balance
    };
}
