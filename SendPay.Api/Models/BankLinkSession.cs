namespace SendPay.Api.Models;

public enum BankLinkSessionStatus
{
    Pending = 0,
    Linked = 1,
    Failed = 2,
    Expired = 3,
    Cancelled = 4,
}

public class BankLinkSession
{
    public int     Id         { get; set; }
    public int     UserId     { get; set; }
    public string  BankCode   { get; set; } = string.Empty;
    public string  State      { get; set; } = string.Empty;
    public BankLinkSessionStatus Status { get; set; } = BankLinkSessionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string?  ReturnUrl  { get; set; }
    public string?  IpAddress  { get; set; }
    public int?     LinkId     { get; set; }

    public User?           User { get; set; }
    public UserBankLink?   Link { get; set; }
}
