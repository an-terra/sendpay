namespace SendPay.Api.Models;

public enum TopUpIntentStatus
{
    Pending = 0,
    Matched = 1,
    Expired = 2,
    Failed = 3,
    Cancelled = 4
}

public class TopUpIntent
{
    public int                Id                   { get; set; }
    public int                UserId               { get; set; }
    public User               User                 { get; set; } = null!;
    public decimal            ExpectedAmount       { get; set; }
    public string             ReferenceCode        { get; set; } = "";
    public TopUpIntentStatus  Status              { get; set; }
    public DateTime           CreatedAt           { get; set; } = DateTime.UtcNow;
    public DateTime           ExpiresAt           { get; set; }
    public DateTime?          MatchedAt           { get; set; }
    public int?               TransactionId        { get; set; }
    public Transaction?       Transaction          { get; set; }
    public int?               BankStatementLineId  { get; set; }
    public BankStatementLine? BankStatementLine    { get; set; }
}
