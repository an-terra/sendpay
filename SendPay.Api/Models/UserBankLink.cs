namespace SendPay.Api.Models;

public class UserBankLink
{
    public int     Id            { get; set; }
    public int     UserId        { get; set; }
    public string  BankCode      { get; set; } = string.Empty;
    public string  BankName      { get; set; } = string.Empty;
    public string  AccountMasked { get; set; } = string.Empty;
    public string  ProviderRef   { get; set; } = string.Empty;
    public string  Provider      { get; set; } = "fake";
    public bool    IsPrimary     { get; set; }
    public bool    IsActive      { get; set; } = true;
    public DateTime LinkedAt     { get; set; } = DateTime.UtcNow;
    public DateTime? UnlinkedAt  { get; set; }

    public User? User { get; set; }
}
