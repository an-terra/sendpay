namespace SendPay.Api.Models;

public class Recipient
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Phone     { get; set; } = string.Empty;
    public string   Note      { get; set; } = string.Empty;
    /// <summary>Mã quốc gia nơi nhận (VN, LK, NP, PH hoặc OTHER). SWIFT gán nội bộ khi khớp catalog.</summary>
    public string?  CountryCode         { get; set; }
    public string?  BankName          { get; set; }
    public string?  AccountNumber     { get; set; }
    public string?  AccountHolderName { get; set; }
    /// <summary>Mã SWIFT/BIC ngân hàng thụ hưởng (chuyển khoản quốc tế).</summary>
    public string?  SwiftBic           { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
