using SendPay.Api.Models;

namespace SendPay.Api.DTOs.Transaction;

public class TransactionResponse
{
    public int               Id           { get; set; }
    public int               SenderId     { get; set; }
    public string            SenderName   { get; set; } = string.Empty;
    public string            ReceiverName { get; set; } = string.Empty;
    public decimal           Amount       { get; set; }
    public decimal           Fee          { get; set; }
    public string            Note         { get; set; } = string.Empty;
    public string?           ReceiverBankName       { get; set; }
    public string?           ReceiverAccountNumber   { get; set; }
    public TransactionType   Type         { get; set; }
    public TransactionStatus Status       { get; set; }
    public DateTime          CreatedAt    { get; set; }
}
