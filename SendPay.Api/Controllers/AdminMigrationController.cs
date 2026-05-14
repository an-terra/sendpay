using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.Models;

namespace SendPay.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/migration")]
public class AdminMigrationController(AppDbContext pg, IConfiguration config, ILogger<AdminMigrationController> log) : ControllerBase
{
    [HttpPost("import-sqlite")]
    [RequestSizeLimit(200 * 1024 * 1024)] // 200MB
    public async Task<IActionResult> ImportSqlite(IFormFile file, [FromQuery] bool wipe = false)
    {
        if (!config.GetValue("Migration:EnableSqliteImport", false))
            return NotFound();
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Thiếu file SQLite." });

        var tmpPath = Path.Combine(Path.GetTempPath(), $"import_{Guid.NewGuid():N}.db");
        try
        {
            await using (var fs = System.IO.File.Create(tmpPath))
                await file.CopyToAsync(fs);

            var sqliteOpts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={tmpPath}")
                .Options;
            await using var sqlite = new AppDbContext(sqliteOpts);

            var existsCheck = await sqlite.Users.AnyAsync();
            if (!existsCheck)
                return BadRequest(new { message = "File SQLite không có bảng Users hoặc trống." });

            // ── Wipe Postgres (giữ admin) ─────────────────────
            if (wipe)
            {
                pg.Transactions.RemoveRange(pg.Transactions);
                pg.Recipients.RemoveRange(pg.Recipients);
                var nonAdmins = pg.Users.Where(u => !u.IsAdmin);
                pg.Users.RemoveRange(nonAdmins);
                await pg.SaveChangesAsync();
                log.LogInformation("Đã wipe data Postgres (giữ admin).");
            }

            // ── Map oldId (SQLite) → newId (Postgres) ──────────
            var idMap = new Dictionary<int, int>();
            var sqliteUsers = await sqlite.Users.AsNoTracking().ToListAsync();

            int upsertedUsers = 0;
            foreach (var su in sqliteUsers)
            {
                var existing = await pg.Users
                    .FirstOrDefaultAsync(x => x.Email == su.Email || x.Phone == su.Phone);

                if (existing is not null)
                {
                    existing.FullName     = su.FullName;
                    existing.Email        = su.Email;
                    existing.Phone        = su.Phone;
                    existing.PasswordHash = su.PasswordHash;
                    existing.Balance      = su.Balance;
                    existing.CreatedAt    = DateTime.SpecifyKind(su.CreatedAt, DateTimeKind.Utc);
                    existing.IsActive     = su.IsActive;
                    if (!existing.IsAdmin) existing.IsAdmin = su.IsAdmin;
                    idMap[su.Id] = existing.Id;
                }
                else
                {
                    var newUser = new User
                    {
                        FullName     = su.FullName,
                        Email        = su.Email,
                        Phone        = su.Phone,
                        PasswordHash = su.PasswordHash,
                        Balance      = su.Balance,
                        CreatedAt    = DateTime.SpecifyKind(su.CreatedAt, DateTimeKind.Utc),
                        IsActive     = su.IsActive,
                        IsAdmin      = su.IsAdmin
                    };
                    pg.Users.Add(newUser);
                    await pg.SaveChangesAsync();
                    idMap[su.Id] = newUser.Id;
                }
                upsertedUsers++;
            }
            await pg.SaveChangesAsync();

            // ── Recipients ────────────────────────────────────
            var sqliteRecipients = await sqlite.Recipients.AsNoTracking().ToListAsync();
            int addedRecipients = 0;
            foreach (var sr in sqliteRecipients)
            {
                if (!idMap.TryGetValue(sr.UserId, out var newUserId)) continue;

                var dup = await pg.Recipients
                    .AnyAsync(x => x.UserId == newUserId && x.Phone == sr.Phone);
                if (dup) continue;

                pg.Recipients.Add(new Recipient
                {
                    UserId    = newUserId,
                    Name      = sr.Name,
                    Phone     = sr.Phone,
                    Note      = sr.Note,
                    CreatedAt = DateTime.SpecifyKind(sr.CreatedAt, DateTimeKind.Utc)
                });
                addedRecipients++;
            }
            await pg.SaveChangesAsync();

            // ── Transactions ──────────────────────────────────
            var sqliteTxns = await sqlite.Transactions.AsNoTracking().ToListAsync();
            int addedTxns = 0;
            int skippedTxns = 0;
            foreach (var st in sqliteTxns)
            {
                if (!idMap.TryGetValue(st.SenderId, out var newSenderId) ||
                    !idMap.TryGetValue(st.ReceiverId, out var newReceiverId))
                {
                    skippedTxns++;
                    continue;
                }

                var createdAt = DateTime.SpecifyKind(st.CreatedAt, DateTimeKind.Utc);
                var dup = await pg.Transactions.AnyAsync(x =>
                    x.SenderId == newSenderId &&
                    x.ReceiverId == newReceiverId &&
                    x.Amount == st.Amount &&
                    x.CreatedAt == createdAt);
                if (dup) { skippedTxns++; continue; }

                pg.Transactions.Add(new Transaction
                {
                    SenderId   = newSenderId,
                    ReceiverId = newReceiverId,
                    Amount     = st.Amount,
                    Fee        = st.Fee,
                    Note       = st.Note,
                    Type       = st.Type,
                    Status     = st.Status,
                    CreatedAt  = createdAt
                });
                addedTxns++;
            }
            await pg.SaveChangesAsync();

            return Ok(new
            {
                message = "Import thành công.",
                wipe,
                users         = new { source = sqliteUsers.Count, upserted = upsertedUsers },
                recipients    = new { source = sqliteRecipients.Count, added = addedRecipients },
                transactions  = new { source = sqliteTxns.Count, added = addedTxns, skipped = skippedTxns }
            });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Lỗi import SQLite");
            return StatusCode(500, new { message = "Lỗi import: " + ex.Message });
        }
        finally
        {
            if (System.IO.File.Exists(tmpPath))
                System.IO.File.Delete(tmpPath);
        }
    }
}
