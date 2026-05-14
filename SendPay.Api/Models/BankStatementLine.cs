namespace SendPay.Api.Models;

public class BankStatementLine
{
    public int            Id                     { get; set; }
    public DateTime       BookingDate          { get; set; }
    public decimal        Amount               { get; set; }
    public string         Memo                 { get; set; } = "";
    public string?        CreditAccountNumber  { get; set; }
    public bool           IsMatched            { get; set; }
    public DateTime       CreatedAt            { get; set; } = DateTime.UtcNow;
    public string         Source               { get; set; } = "AdminImport";
}
