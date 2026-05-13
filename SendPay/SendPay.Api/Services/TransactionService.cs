using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Transaction;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class TransactionService(AppDbContext db) : ITransactionService
{
    public async Task<TransactionResponse> TransferAsync(int senderId, TransferRequest req)
    {
        var sender = await db.Users.FindAsync(senderId)
            ?? throw new KeyNotFoundException("Người gửi không tồn tại.");

        var receiver = await db.Users.FirstOrDefaultAsync(u => u.Phone == req.ReceiverPhone)
            ?? throw new KeyNotFoundException($"Không tìm thấy số điện thoại {req.ReceiverPhone}.");

        if (sender.Id == receiver.Id)
            throw new InvalidOperationException("Không thể chuyển tiền cho chính mình.");

        decimal fee = req.Amount <= 10_000 ? 200 : req.Amount <= 50_000 ? 400 : 800;
        decimal total = req.Amount + fee;

        if (sender.Balance < total)
            throw new InvalidOperationException(
                $"Số dư không đủ. Cần ¥{total:N0} (bao gồm phí ¥{fee:N0}), hiện có: ¥{sender.Balance:N0}.");

        // Người gửi trừ cả phí, người nhận nhận đúng số tiền
        sender.Balance   -= total;
        receiver.Balance += req.Amount;

        var tx = new Transaction
        {
            SenderId   = sender.Id,
            ReceiverId = receiver.Id,
            Amount     = req.Amount,
            Note       = req.Note,
            Type       = TransactionType.Transfer,
            Status     = TransactionStatus.Success
        };

        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        return ToResponse(tx, sender.FullName, receiver.FullName);
    }

    public async Task<TransactionResponse> GetByIdAsync(int userId, int id)
    {
        var t = await db.Transactions
            .Include(x => x.Sender).Include(x => x.Receiver)
            .FirstOrDefaultAsync(x => x.Id == id && (x.SenderId == userId || x.ReceiverId == userId))
            ?? throw new KeyNotFoundException("Không tìm thấy giao dịch.");
        return ToResponse(t, t.Sender.FullName, t.Receiver.FullName);
    }

    public async Task<List<TransactionResponse>> GetHistoryAsync(int userId, int page, int pageSize)
    {
        var txs = await db.Transactions
            .Include(t => t.Sender)
            .Include(t => t.Receiver)
            .Where(t => t.SenderId == userId || t.ReceiverId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return txs.Select(t => ToResponse(t, t.Sender.FullName, t.Receiver.FullName)).ToList();
    }

    private static TransactionResponse ToResponse(Transaction t, string senderName, string receiverName) => new()
    {
        Id           = t.Id,
        SenderId     = t.SenderId,
        SenderName   = senderName,
        ReceiverName = receiverName,
        Amount       = t.Amount,
        Note         = t.Note,
        Type         = t.Type,
        Status       = t.Status,
        CreatedAt    = t.CreatedAt
    };
}
