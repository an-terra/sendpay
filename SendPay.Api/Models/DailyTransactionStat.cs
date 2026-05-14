namespace SendPay.Api.Models;

public class DailyTransactionStat
{
    public int                Id              { get; set; }
    public DateTime           StatDate        { get; set; }
    public TransactionType    TransactionType { get; set; }
    public TransactionStatus  Status          { get; set; }
    public int                  Count           { get; set; }
    public decimal            TotalAmount     { get; set; }
    public decimal            TotalFee        { get; set; }
    public DateTime           ComputedAt      { get; set; } = DateTime.UtcNow;
}
