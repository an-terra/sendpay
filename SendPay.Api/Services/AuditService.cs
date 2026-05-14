using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class AuditService(AppDbContext db, ILogger<AuditService> logger) : IAuditService
{
    public async Task WriteAsync(string action, string detail, int? userId, string? ipAddress, CancellationToken ct = default)
    {
        if (action.Length > 64) action = action[..64];
        if (detail.Length > 512) detail = detail[..509] + "…";

        try
        {
            db.AuditLogs.Add(new AuditLog
            {
                CreatedAtUtc = DateTime.UtcNow,
                UserId       = userId,
                Action       = action,
                Detail       = detail,
                IpAddress    = ipAddress
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit write failed: {Action}", action);
        }
    }
}
