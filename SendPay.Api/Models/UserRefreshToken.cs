namespace SendPay.Api.Models;

public class UserRefreshToken
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string?  IpAddress  { get; set; }

    public User? User { get; set; }
}
