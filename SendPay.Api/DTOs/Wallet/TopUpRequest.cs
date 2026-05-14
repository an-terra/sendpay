using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Wallet;

public class TopUpRequest
{
    [Required] public Guid VerificationId { get; set; }

    /// <summary>Mã 6 chữ số đã gửi qua kênh xác thực (production: SMS/email).</summary>
    [Required, StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;

    [Required, Range(1000, 100_000_000)]
    public decimal Amount { get; set; }
}
