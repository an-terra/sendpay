namespace SendPay.Api.Models;

public class AuditLog
{
    public long     Id           { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int?     UserId       { get; set; }
    public string   Action       { get; set; } = "";
    public string   Detail       { get; set; } = "";
    public string?  IpAddress    { get; set; }
}
