using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Wallet;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class WalletService(AppDbContext db) : IWalletService
{
    public async Task<WalletResponse> GetBalanceAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        return ToResponse(user);
    }

    public async Task<WalletResponse> TopUpAsync(int userId, TopUpRequest req)
    {
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
