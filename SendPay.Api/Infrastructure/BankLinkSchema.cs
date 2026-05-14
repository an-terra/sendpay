using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;

namespace SendPay.Api.Infrastructure;

public static class BankLinkSchema
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
            // ignore — bảng có thể đã tồn tại hoặc provider khác
        }
    }

    static void EnsureSqlite(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "UserBankLinks" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "BankCode" TEXT NOT NULL,
                "BankName" TEXT NOT NULL,
                "AccountMasked" TEXT NOT NULL,
                "ProviderRef" TEXT NOT NULL,
                "Provider" TEXT NOT NULL DEFAULT 'fake',
                "IsPrimary" INTEGER NOT NULL DEFAULT 0,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "LinkedAt" TEXT NOT NULL,
                "UnlinkedAt" TEXT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserBankLinks_User_Active" ON "UserBankLinks" ("UserId", "IsActive");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "BankLinkSessions" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "BankCode" TEXT NOT NULL,
                "State" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "CompletedAt" TEXT NULL,
                "ReturnUrl" TEXT NULL,
                "IpAddress" TEXT NULL,
                "LinkId" INTEGER NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_BankLinkSessions_State" ON "BankLinkSessions" ("State");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_BankLinkSessions_User_Status" ON "BankLinkSessions" ("UserId", "Status");
            """);
    }

    static void EnsurePostgres(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "UserBankLinks" (
                "Id" serial PRIMARY KEY,
                "UserId" integer NOT NULL,
                "BankCode" text NOT NULL,
                "BankName" text NOT NULL,
                "AccountMasked" text NOT NULL,
                "ProviderRef" text NOT NULL,
                "Provider" text NOT NULL DEFAULT 'fake',
                "IsPrimary" boolean NOT NULL DEFAULT false,
                "IsActive" boolean NOT NULL DEFAULT true,
                "LinkedAt" timestamp with time zone NOT NULL,
                "UnlinkedAt" timestamp with time zone NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_UserBankLinks_User_Active" ON "UserBankLinks" ("UserId", "IsActive");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "BankLinkSessions" (
                "Id" serial PRIMARY KEY,
                "UserId" integer NOT NULL,
                "BankCode" text NOT NULL,
                "State" text NOT NULL,
                "Status" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone NULL,
                "ReturnUrl" text NULL,
                "IpAddress" text NULL,
                "LinkId" integer NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_BankLinkSessions_State" ON "BankLinkSessions" ("State");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_BankLinkSessions_User_Status" ON "BankLinkSessions" ("UserId", "Status");
            """);
    }
}
