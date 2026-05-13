namespace SendPay.Api.Models;

public class Recipient
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Phone     { get; set; } = string.Empty;
    public string   Note      { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
