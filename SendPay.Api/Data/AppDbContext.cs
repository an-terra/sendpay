using Microsoft.EntityFrameworkCore;
using SendPay.Api.Models;

namespace SendPay.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>                 Users                  => Set<User>();
    public DbSet<Transaction>        Transactions           => Set<Transaction>();
    public DbSet<Recipient>          Recipients             => Set<Recipient>();
    public DbSet<TopUpIntent>        TopUpIntents           => Set<TopUpIntent>();
    public DbSet<BankStatementLine>  BankStatementLines     => Set<BankStatementLine>();
    public DbSet<DailyTransactionStat> DailyTransactionStats => Set<DailyTransactionStat>();
    public DbSet<BankLinkSession>    BankLinkSessions       => Set<BankLinkSession>();
    public DbSet<UserBankLink>       UserBankLinks          => Set<UserBankLink>();
    public DbSet<UserRefreshToken>   UserRefreshTokens      => Set<UserRefreshToken>();
    public DbSet<JwtBlacklistEntry>  JwtBlacklistEntries    => Set<JwtBlacklistEntry>();
    public DbSet<AuditLog>           AuditLogs              => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Phone).IsUnique();
            e.Property(u => u.Balance).HasColumnType("decimal(18,2)");
        });

        mb.Entity<Recipient>(e =>
        {
            e.HasOne(r => r.User)
             .WithMany(u => u.Recipients)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Transaction>(e =>
        {
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Fee).HasColumnType("decimal(18,2)");

            e.HasOne(t => t.Sender)
             .WithMany(u => u.SentTransactions)
             .HasForeignKey(t => t.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Receiver)
             .WithMany(u => u.ReceivedTransactions)
             .HasForeignKey(t => t.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<TopUpIntent>(e =>
        {
            e.Property(x => x.ExpectedAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.ReferenceCode).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Status });
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Transaction)
             .WithMany()
             .HasForeignKey(x => x.TransactionId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.BankStatementLine)
             .WithMany()
             .HasForeignKey(x => x.BankStatementLineId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<BankStatementLine>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        mb.Entity<DailyTransactionStat>(e =>
        {
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalFee).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.StatDate, x.TransactionType, x.Status }).IsUnique();
        });

        mb.Entity<UserBankLink>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsActive });
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<BankLinkSession>(e =>
        {
            e.HasIndex(x => x.State).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Status });
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Link)
             .WithMany()
             .HasForeignKey(x => x.LinkId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<JwtBlacklistEntry>(e => { e.HasKey(x => x.Jti); });

        mb.Entity<UserRefreshToken>(e =>
        {
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
