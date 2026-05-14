namespace SendPay.Api.Models;

public class JwtBlacklistEntry
{
    public string   Jti          { get; set; } = "";
    public int      UserId      { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
