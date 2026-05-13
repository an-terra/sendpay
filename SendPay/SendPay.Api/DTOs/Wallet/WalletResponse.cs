namespace SendPay.Api.DTOs.Wallet;

public class WalletResponse
{
    public string  FullName { get; set; } = string.Empty;
    public string  Phone    { get; set; } = string.Empty;
    public decimal Balance  { get; set; }
}
