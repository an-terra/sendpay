namespace SendPay.Api.DTOs.Wallet;

public class WalletTopUpResponse
{
    /// <summary>instant | bank_pending</summary>
    public string Mode { get; set; } = "";

    public WalletResponse? Wallet { get; set; }
    public int? IntentId { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal? ExpectedAmount { get; set; }
}
