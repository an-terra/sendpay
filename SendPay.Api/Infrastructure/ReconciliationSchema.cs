using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;

namespace SendPay.Api.Infrastructure;

public static class ReconciliationSchema
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
            // ignore — có thể đã tồn tại hoặc provider khác
        }
    }

    static void EnsureSqlite(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "BankStatementLines" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "BookingDate" TEXT NOT NULL,
                "Amount" TEXT NOT NULL,
                "Memo" TEXT NOT NULL,
                "CreditAccountNumber" TEXT NULL,
                "IsMatched" INTEGER NOT NULL DEFAULT 0,
                "CreatedAt" TEXT NOT NULL,
                "Source" TEXT NOT NULL DEFAULT 'AdminImport'
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "TopUpIntents" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "ExpectedAmount" TEXT NOT NULL,
                "ReferenceCode" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "MatchedAt" TEXT NULL,
                "TransactionId" INTEGER NULL,
                "BankStatementLineId" INTEGER NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TopUpIntents_ReferenceCode" ON "TopUpIntents" ("ReferenceCode");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_TopUpIntents_UserId_Status" ON "TopUpIntents" ("UserId", "Status");
            """);
        db.Database
            .ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "DailyTransactionStats" (
                    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "StatDate" TEXT NOT NULL,
                    "TransactionType" INTEGER NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "Count" INTEGER NOT NULL,
                    "TotalAmount" TEXT NOT NULL,
                    "TotalFee" TEXT NOT NULL,
                    "ComputedAt" TEXT NOT NULL
                );
                """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DailyTransactionStats_key" ON "DailyTransactionStats" ("StatDate", "TransactionType", "Status");
            """);
    }

    static void EnsurePostgres(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "BankStatementLines" (
                "Id" serial PRIMARY KEY,
                "BookingDate" timestamp with time zone NOT NULL,
                "Amount" numeric(18,2) NOT NULL,
                "Memo" text NOT NULL,
                "CreditAccountNumber" text NULL,
                "IsMatched" boolean NOT NULL DEFAULT false,
                "CreatedAt" timestamp with time zone NOT NULL,
                "Source" text NOT NULL DEFAULT 'AdminImport'
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "TopUpIntents" (
                "Id" serial PRIMARY KEY,
                "UserId" integer NOT NULL,
                "ExpectedAmount" numeric(18,2) NOT NULL,
                "ReferenceCode" text NOT NULL,
                "Status" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "MatchedAt" timestamp with time zone NULL,
                "TransactionId" integer NULL,
                "BankStatementLineId" integer NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TopUpIntents_ReferenceCode" ON "TopUpIntents" ("ReferenceCode");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_TopUpIntents_UserId_Status" ON "TopUpIntents" ("UserId", "Status");
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DailyTransactionStats" (
                "Id" serial PRIMARY KEY,
                "StatDate" timestamp with time zone NOT NULL,
                "TransactionType" integer NOT NULL,
                "Status" integer NOT NULL,
                "Count" integer NOT NULL,
                "TotalAmount" numeric(18,2) NOT NULL,
                "TotalFee" numeric(18,2) NOT NULL,
                "ComputedAt" timestamp with time zone NOT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DailyTransactionStats_key" ON "DailyTransactionStats" ("StatDate", "TransactionType", "Status");
            """);
    }
}
