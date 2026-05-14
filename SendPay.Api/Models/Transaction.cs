namespace SendPay.Api.Models;

public enum TransactionType   { Transfer, TopUp, Withdraw }
public enum TransactionStatus { Pending, Success, Failed }

public class Transaction
{
    public int               Id         { get; set; }
    public int               SenderId   { get; set; }
    public int               ReceiverId { get; set; }
    public decimal           Amount     { get; set; }
    public decimal           Fee        { get; set; }
    public string            Note       { get; set; } = string.Empty;
    /// <summary>Ngân hàng thụ hưởng do người gửi khai báo (ghi nhận khi chuyển).</summary>
    public string?           ReceiverBankName     { get; set; }
    /// <summary>Số tài khoản thụ hưởng (ghi nhận khi chuyển).</summary>
    public string?           ReceiverAccountNumber { get; set; }
    public TransactionType   Type       { get; set; }
    public TransactionStatus Status     { get; set; } = TransactionStatus.Success;
    public DateTime          CreatedAt  { get; set; } = DateTime.UtcNow;

    public User Sender   { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
