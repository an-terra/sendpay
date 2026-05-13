namespace SendPay.Api.Models;

public enum TransactionType   { Transfer, TopUp, Withdraw }
public enum TransactionStatus { Pending, Success, Failed }

public class Transaction
{
    public int               Id         { get; set; }
    public int               SenderId   { get; set; }
    public int               ReceiverId { get; set; }
    public decimal           Amount     { get; set; }
    public string            Note       { get; set; } = string.Empty;
    public TransactionType   Type       { get; set; }
    public TransactionStatus Status     { get; set; } = TransactionStatus.Success;
    public DateTime          CreatedAt  { get; set; } = DateTime.UtcNow;

    public User Sender   { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
