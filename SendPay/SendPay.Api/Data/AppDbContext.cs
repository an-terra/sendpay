using Microsoft.EntityFrameworkCore;
using SendPay.Api.Models;

namespace SendPay.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>        Users        => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Recipient>   Recipients   => Set<Recipient>();

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

            e.HasOne(t => t.Sender)
             .WithMany(u => u.SentTransactions)
             .HasForeignKey(t => t.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Receiver)
             .WithMany(u => u.ReceivedTransactions)
             .HasForeignKey(t => t.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
