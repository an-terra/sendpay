using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class ReconciliationService(AppDbContext db) : IReconciliationService
{
    public async Task<int> MatchBankCreditsAsync(CancellationToken ct = default)
    {
        var lines = await db.BankStatementLines
            .Where(l => !l.IsMatched)
            .OrderBy(l => l.Id)
            .ToListAsync(ct);

        var n = 0;
        foreach (var line in lines)
        {
            if (await TryMatchLineAsync(line, ct))
                n++;
        }

        return n;
    }

    async Task<bool> TryMatchLineAsync(BankStatementLine line, CancellationToken ct)
    {
        var intents = await db.TopUpIntents
            .Where(i => i.Status == TopUpIntentStatus.Pending
                        && i.ExpectedAmount == line.Amount
                        && i.ExpiresAt > DateTime.UtcNow)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        foreach (var intent in intents)
        {
            if (!line.Memo.Contains(intent.ReferenceCode, StringComparison.OrdinalIgnoreCase))
                continue;

            await using var trx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var lineRow = await db.BankStatementLines
                    .FirstOrDefaultAsync(l => l.Id == line.Id && !l.IsMatched, ct);
                var intentRow = await db.TopUpIntents
                    .FirstOrDefaultAsync(i => i.Id == intent.Id && i.Status == TopUpIntentStatus.Pending, ct);
                if (lineRow is null || intentRow is null)
                {
                    await trx.RollbackAsync(ct);
                    return false;
                }

                await ApplyCreditAsync(intentRow, lineRow, db, ct);
                await trx.CommitAsync(ct);
                return true;
            }
            catch
            {
                await trx.RollbackAsync(ct);
                return false;
            }
        }

        return false;
    }

    static async Task ApplyCreditAsync(TopUpIntent intent, BankStatementLine? line, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == intent.UserId, ct)
            ?? throw new InvalidOperationException("User not found");

        user.Balance += intent.ExpectedAmount;

        var tx = new Transaction
        {
            SenderId = user.Id,
            ReceiverId = user.Id,
            Amount = intent.ExpectedAmount,
            Fee = 0,
            Type = TransactionType.TopUp,
            Status = TransactionStatus.Success,
            Note = line is null
                ? $"Nạp tiền (xác nhận admin) · {intent.ReferenceCode}"
                : $"Nạp tiền · {intent.ReferenceCode}",
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(ct);

        intent.Status = TopUpIntentStatus.Matched;
        intent.MatchedAt = DateTime.UtcNow;
        intent.TransactionId = tx.Id;
        if (line is not null)
        {
            intent.BankStatementLineId = line.Id;
            line.IsMatched = true;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ExpireStaleTopUpIntentsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var stale = await db.TopUpIntents
            .Where(i => i.Status == TopUpIntentStatus.Pending && i.ExpiresAt < now)
            .ToListAsync(ct);
        foreach (var i in stale)
            i.Status = TopUpIntentStatus.Expired;
        if (stale.Count > 0)
            await db.SaveChangesAsync(ct);
        return stale.Count;
    }

    public async Task RebuildDailyStatsForUtcDateAsync(DateTime utcDayStart, CancellationToken ct = default)
    {
        var start = utcDayStart.Kind == DateTimeKind.Utc
            ? utcDayStart.Date
            : DateTime.SpecifyKind(utcDayStart.Date, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var old = await db.DailyTransactionStats.Where(s => s.StatDate == start).ToListAsync(ct);
        if (old.Count > 0)
            db.DailyTransactionStats.RemoveRange(old);

        var groups = await db.Transactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= start && t.CreatedAt < end)
            .GroupBy(t => new { t.Type, t.Status })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Status,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                TotalFee = g.Sum(x => x.Fee)
            })
            .ToListAsync(ct);

        var computed = DateTime.UtcNow;
        foreach (var g in groups)
        {
            db.DailyTransactionStats.Add(new DailyTransactionStat
            {
                StatDate = start,
                TransactionType = g.Type,
                Status = g.Status,
                Count = g.Count,
                TotalAmount = g.TotalAmount,
                TotalFee = g.TotalFee,
                ComputedAt = computed
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<(bool ok, string error)> AdminConfirmTopUpAsync(int intentId, CancellationToken ct = default)
    {
        await using var trx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var intent = await db.TopUpIntents
                .FirstOrDefaultAsync(i => i.Id == intentId, ct);
            if (intent is null)
            {
                await trx.RollbackAsync(ct);
                return (false, "Không tìm thấy lệnh nạp.");
            }

            if (intent.Status != TopUpIntentStatus.Pending)
            {
                await trx.RollbackAsync(ct);
                return (false, "Lệnh không ở trạng thái chờ xử lý.");
            }

            await ApplyCreditAsync(intent, null, db, ct);
            await trx.CommitAsync(ct);
            return (true, "");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync(ct);
            return (false, ex.Message);
        }
    }
}
