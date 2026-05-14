using System.Data;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Transaction;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class TransactionService(AppDbContext db) : ITransactionService
{
    public async Task<TransactionResponse> TransferAsync(int senderId, TransferRequest req)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var sender = await db.Users.AsNoTracking()
                .Where(u => u.Id == senderId)
                .Select(u => new { u.Id, u.FullName, u.Balance })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Người gửi không tồn tại.");

            var normalized = OtpPayloadBuilder.NormalizePhone(req.ReceiverPhone);
            var receiver = await db.Users.AsNoTracking()
                .Where(u => u.Phone == req.ReceiverPhone || OtpPayloadBuilder.NormalizePhone(u.Phone) == normalized)
                .Select(u => new { u.Id, u.FullName })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Không tìm thấy số điện thoại {req.ReceiverPhone}.");

            if (sender.Id == receiver.Id)
                throw new InvalidOperationException("Không thể chuyển tiền cho chính mình.");

            decimal fee = req.Amount <= 10_000 ? 200 : req.Amount <= 50_000 ? 400 : 800;
            decimal total = req.Amount + fee;

            if (sender.Balance < total)
                throw new InvalidOperationException(
                    $"Số dư không đủ. Cần ¥{total:N0} (bao gồm phí ¥{fee:N0}), hiện có: ¥{sender.Balance:N0}.");

            var rows = await db.Database.ExecuteSqlAsync(
                $"""
                 UPDATE "Users" SET "Balance" = "Balance" - {total}
                 WHERE "Id" = {senderId} AND "Balance" >= {total}
                 """);
            if (rows != 1)
                throw new InvalidOperationException("Không thể hoàn tất giao dịch (số dư đã thay đổi). Hãy thử lại.");

            await db.Database.ExecuteSqlAsync(
                $"""
                 UPDATE "Users" SET "Balance" = "Balance" + {req.Amount}
                 WHERE "Id" = {receiver.Id}
                 """);

            var entity = new Transaction
            {
                SenderId              = sender.Id,
                ReceiverId            = receiver.Id,
                Amount                = req.Amount,
                Fee                   = fee,
                Note                  = req.Note,
                ReceiverBankName      = string.IsNullOrWhiteSpace(req.ReceiverBankName) ? null : req.ReceiverBankName.Trim(),
                ReceiverAccountNumber = string.IsNullOrWhiteSpace(req.ReceiverAccountNumber) ? null : req.ReceiverAccountNumber.Trim(),
                Type                  = TransactionType.Transfer,
                Status                = TransactionStatus.Success
            };
            db.Transactions.Add(entity);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return ToResponse(entity, sender.FullName, receiver.FullName);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
        Fee          = t.Fee,
        Note                  = t.Note,
        ReceiverBankName      = t.ReceiverBankName,
        ReceiverAccountNumber = t.ReceiverAccountNumber,
        Type         = t.Type,
        Status       = t.Status,
        CreatedAt    = t.CreatedAt
    };
}
