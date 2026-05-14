using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Transaction;

public class TransferRequest
{
    [Required] public Guid VerificationId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;

    [Required] public string ReceiverPhone { get; set; } = string.Empty;
    [Required, Range(1000, 100_000_000)] public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
