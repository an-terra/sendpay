namespace SendPay.Api.Models;

/// <summary>
/// Mã OTP một lần cho nạp / chuyển tiền (6 số, TTL ngắn).
/// Production: gửi SMS/email thật; hiện tại lưu hash + có debug trong Dev.
/// </summary>
public class OtpChallenge
{
    public Guid      Id          { get; set; }
    public int       UserId      { get; set; }
    /// <summary>JSON canonical khớp payload lúc start và lúc xác nhận.</summary>
    public string    PayloadJson { get; set; } = string.Empty;
    public string    CodeHash    { get; set; } = string.Empty;
    public DateTime  ExpiresAt   { get; set; }
    public DateTime? ConsumedAt  { get; set; }

    public User User { get; set; } = null!;
}
