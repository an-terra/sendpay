using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;

namespace SendPay.Api.Infrastructure;

public static class SecuritySchema
{
    public static void EnsureTables(AppDbContext db)
    {
        var isSqlite = db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? false;
        try
        {
            if (isSqlite) EnsureSqlite(db);
            else EnsurePostgres(db);
        }
        catch
        {
            // ignore
        }
    }

    static void EnsureSqlite(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "UserRefreshTokens" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "TokenHash" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "RevokedAt" TEXT NULL,
                "IpAddress" TEXT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserRefreshTokens_UserId" ON "UserRefreshTokens" ("UserId");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserRefreshTokens_TokenHash" ON "UserRefreshTokens" ("TokenHash");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "JwtBlacklistEntries" (
                "Jti" TEXT NOT NULL PRIMARY KEY,
                "UserId" INTEGER NOT NULL,
                "ExpiresAtUtc" TEXT NOT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "AuditLogs" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "CreatedAtUtc" TEXT NOT NULL,
                "UserId" INTEGER NULL,
                "Action" TEXT NOT NULL,
                "Detail" TEXT NOT NULL,
                "IpAddress" TEXT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAtUtc");
            """);
    }

    static void EnsurePostgres(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "UserRefreshTokens" (
                "Id" serial PRIMARY KEY,
                "UserId" integer NOT NULL,
                "TokenHash" text NOT NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "RevokedAt" timestamp with time zone NULL,
                "IpAddress" text NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserRefreshTokens_UserId" ON "UserRefreshTokens" ("UserId");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserRefreshTokens_TokenHash" ON "UserRefreshTokens" ("TokenHash");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "JwtBlacklistEntries" (
                "Jti" text NOT NULL PRIMARY KEY,
                "UserId" integer NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "AuditLogs" (
                "Id" bigserial PRIMARY KEY,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UserId" integer NULL,
                "Action" text NOT NULL,
                "Detail" text NOT NULL,
                "IpAddress" text NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAtUtc");
            """);
    }
}
