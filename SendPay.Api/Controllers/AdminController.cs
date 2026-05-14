using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Admin;
using SendPay.Api.Models;

namespace SendPay.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController(AppDbContext db) : ControllerBase
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
}
