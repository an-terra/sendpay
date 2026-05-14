using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class RefreshTokenService(AppDbContext db, IConfiguration config) : IRefreshTokenService
{
    int RefreshDays => config.GetValue("Jwt:RefreshTokenDays", 14);
    public async Task<(string plainToken, UserRefreshToken entity)> IssueAsync(
        int userId, string? ipAddress, CancellationToken ct = default)
    {
        var plain = GenerateToken();
        var hash = Hash(plain);
        var entity = new UserRefreshToken
        {
            UserId    = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshDays),
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
        };
        db.UserRefreshTokens.Add(entity);
        await db.SaveChangesAsync(ct);
        return (plain, entity);
    }

    public async Task<(User user, string plainRefresh)?> ValidateAndRotateAsync(
        string plainRefresh, string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainRefresh)) return null;
        var hash = Hash(plainRefresh.Trim());
        var row = await db.UserRefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (row is null) return null;
        if (row.RevokedAt.HasValue) return null;
        if (row.ExpiresAt < DateTime.UtcNow) return null;
        if (!row.User!.IsActive) return null;

        row.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var (newPlain, _) = await IssueAsync(row.UserId, ipAddress, ct);
        return (row.User, newPlain);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await db.UserRefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public async Task RevokeEntityAsync(UserRefreshToken token, CancellationToken ct = default)
    {
        token.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    static string GenerateToken()
    {
        Span<byte> b = stackalloc byte[48];
        RandomNumberGenerator.Fill(b);
        return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToBase64String(bytes);
    }
}
