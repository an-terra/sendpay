namespace SendPay.Api.Models;

public class Recipient
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Phone     { get; set; } = string.Empty;
    public string   Note      { get; set; } = string.Empty;
    /// <summary>Chi trả VN (mô phỏng).</summary>
    public string?  BankName          { get; set; }
    public string?  AccountNumber     { get; set; }
    public string?  AccountHolderName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
