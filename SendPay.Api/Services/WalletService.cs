using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Wallet;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class WalletService(AppDbContext db, IWebHostEnvironment env, IConfiguration config) : IWalletService
{
    public async Task<WalletResponse> GetBalanceAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        return ToResponse(user);
    }

    public async Task<WalletResponse> TopUpAsync(int userId, TopUpRequest req)
    {
        if (!env.IsDevelopment() && !config.GetValue("Features:AllowDemoTopUp", false))
            throw new InvalidOperationException(
                "Nạp tiền qua API đã tắt trên production. Hãy bật Features:AllowDemoTopUp (chỉ demo) hoặc tích hợp cổng thanh toán thật.");

        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        user.Balance += req.Amount;

        db.Transactions.Add(new Transaction
        {
            SenderId   = userId,
            ReceiverId = userId,
            Amount     = req.Amount,
            Type       = TransactionType.TopUp,
            Note       = "Nạp tiền vào ví"
        });

        await db.SaveChangesAsync();
        return ToResponse(user);
    }

    private static WalletResponse ToResponse(User u) => new()
    {
        FullName = u.FullName,
        Phone    = u.Phone,
        Balance  = u.Balance
    };
}
