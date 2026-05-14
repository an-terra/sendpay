using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Admin;
using SendPay.Api.Models;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController(AppDbContext db, IReconciliationService reconciliation) : ControllerBase
{
    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<AdminStatsResponse> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        return new AdminStatsResponse
        {
            TotalUsers        = await db.Users.CountAsync(),
            ActiveUsers       = await db.Users.CountAsync(u => u.IsActive),
            TotalTransactions = await db.Transactions.CountAsync(),
            TotalVolume       = await db.Transactions
                                    .Where(t => t.Type == TransactionType.Transfer)
                                    .SumAsync(t => (decimal?)t.Amount) ?? 0,
            TodayTransactions = await db.Transactions.CountAsync(t => t.CreatedAt >= today),
            TodayVolume       = await db.Transactions
                                    .Where(t => t.CreatedAt >= today && t.Type == TransactionType.Transfer)
                                    .SumAsync(t => (decimal?)t.Amount) ?? 0
        };
    }

    // GET /api/admin/users?page=1&pageSize=15&search=
    [HttpGet("users")]
    public async Task<List<AdminUserResponse>> GetUsers(
        int page = 1, int pageSize = 15, string? search = null)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.FullName.Contains(search)
                          || u.Email.Contains(search)
                          || u.Phone.Contains(search));

        return await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserResponse(
                u.Id, u.FullName, u.Email, u.Phone,
                u.Balance, u.IsActive, u.IsAdmin, u.CreatedAt,
                u.JapanBankName, u.JapanBankTopUpUrl))
            .ToListAsync();
    }

    // PUT /api/admin/users/{id}/toggle
    [HttpPut("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.IsAdmin) return BadRequest(new { message = "Không thể khóa tài khoản admin." });

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();

        return Ok(new AdminUserResponse(
            user.Id, user.FullName, user.Email, user.Phone,
            user.Balance, user.IsActive, user.IsAdmin, user.CreatedAt,
            user.JapanBankName, user.JapanBankTopUpUrl));
    }

    // PUT /api/admin/users/{id}
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, AdminUpdateUserRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.FullName = req.FullName.Trim();
        user.Email    = req.Email.Trim();
        user.Phone    = req.Phone.Trim();
        if (req.Balance.HasValue && req.Balance >= 0)
            user.Balance = req.Balance.Value;

        user.JapanBankName = string.IsNullOrWhiteSpace(req.JapanBankName)
            ? null
            : req.JapanBankName.Trim();

        if (string.IsNullOrWhiteSpace(req.JapanBankTopUpUrl))
            user.JapanBankTopUpUrl = null;
        else
        {
            var url = req.JapanBankTopUpUrl.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "URL ngân hàng phải bắt đầu bằng http:// hoặc https://." });
            user.JapanBankTopUpUrl = url;
        }

        await db.SaveChangesAsync();
        return Ok(new AdminUserResponse(
            user.Id, user.FullName, user.Email, user.Phone,
            user.Balance, user.IsActive, user.IsAdmin, user.CreatedAt,
            user.JapanBankName, user.JapanBankTopUpUrl));
    }

    // DELETE /api/admin/users/{id}
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.IsAdmin) return BadRequest(new { message = "Không thể xóa tài khoản admin." });

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa người dùng." });
    }

    // GET /api/admin/transactions?page=1&pageSize=15
    [HttpGet("transactions")]
    public async Task<List<AdminTransactionResponse>> GetTransactions(
        int page = 1, int pageSize = 15)
    {
        return await db.Transactions
            .Include(t => t.Sender)
            .Include(t => t.Receiver)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AdminTransactionResponse(
                t.Id,
                t.Sender.FullName,
                t.Receiver.FullName,
                t.Amount,
                t.Note,
                t.Type.ToString(),
                t.Status.ToString(),
                t.CreatedAt))
            .ToListAsync();
    }

    [HttpGet("topup-intents")]
    public async Task<List<AdminTopUpIntentResponse>> GetTopUpIntents(
        string? status = null, int page = 1, int pageSize = 20)
    {
        var q = db.TopUpIntents.AsNoTracking().Include(i => i.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TopUpIntentStatus>(status, true, out var st))
            q = q.Where(i => i.Status == st);

        return await q
            .OrderByDescending(i => i.CreatedAt)
            .Skip(Math.Max(0, page - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .Select(i => new AdminTopUpIntentResponse(
                i.Id,
                i.UserId,
                i.User.FullName,
                i.User.Email,
                i.ExpectedAmount,
                i.ReferenceCode,
                i.Status.ToString(),
                i.CreatedAt,
                i.ExpiresAt,
                i.MatchedAt,
                i.TransactionId,
                i.BankStatementLineId))
            .ToListAsync();
    }

    [HttpPost("topup-intents/{id}/confirm")]
    public async Task<IActionResult> ConfirmTopUpIntent(int id)
    {
        var (ok, err) = await reconciliation.AdminConfirmTopUpAsync(id);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Đã ghi có ví." });
    }

    [HttpPost("bank-statement-lines")]
    public async Task<IActionResult> ImportBankStatementLines([FromBody] BankStatementImportRequest req)
    {
        if (req.Lines is not { Count: > 0 }) return BadRequest(new { message = "Danh sách trống." });
        var src = "AdminImport";
        foreach (var line in req.Lines)
        {
            var lineSrc = string.IsNullOrWhiteSpace(line.Source) ? src : line.Source.Trim();
            if (lineSrc.Length > 64)
                lineSrc = lineSrc[..64];
            db.BankStatementLines.Add(new BankStatementLine
            {
                BookingDate = line.BookingDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(line.BookingDate, DateTimeKind.Utc)
                    : line.BookingDate.ToUniversalTime(),
                Amount = line.Amount,
                Memo = line.Memo.Trim(),
                CreditAccountNumber = string.IsNullOrWhiteSpace(line.CreditAccountNumber)
                    ? null
                    : line.CreditAccountNumber.Trim(),
                IsMatched = false,
                CreatedAt = DateTime.UtcNow,
                Source = lineSrc
            });
        }

        await db.SaveChangesAsync();
        _ = await reconciliation.MatchBankCreditsAsync();
        return Ok(new { imported = req.Lines.Count });
    }

    [HttpGet("daily-stats")]
    public async Task<List<AdminDailyStatResponse>> GetDailyStats(DateTime? from = null, DateTime? to = null)
    {
        var f = DateTime.SpecifyKind((from ?? DateTime.UtcNow.AddDays(-31)).Date, DateTimeKind.Utc);
        var t = DateTime.SpecifyKind((to ?? DateTime.UtcNow.Date).Date, DateTimeKind.Utc);
        return await db.DailyTransactionStats.AsNoTracking()
            .Where(s => s.StatDate >= f && s.StatDate <= t)
            .OrderBy(s => s.StatDate)
            .ThenBy(s => s.TransactionType)
            .ThenBy(s => s.Status)
            .Select(s => new AdminDailyStatResponse(
                s.StatDate,
                s.TransactionType.ToString(),
                s.Status.ToString(),
                s.Count,
                s.TotalAmount,
                s.TotalFee,
                s.ComputedAt))
            .ToListAsync();
    }

    [HttpPost("daily-stats/rebuild")]
    public async Task<IActionResult> RebuildDailyStats([FromQuery] DateTime? utcDay = null)
    {
        var day = utcDay.HasValue
            ? DateTime.SpecifyKind(utcDay.Value.Date, DateTimeKind.Utc)
            : DateTime.UtcNow.Date.AddDays(-1);
        await reconciliation.RebuildDailyStatsForUtcDateAsync(day);
        return Ok(new { message = $"Đã rebuild thống kê cho {day:yyyy-MM-dd} UTC." });
    }
}
