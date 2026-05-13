using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Transaction;

public class TransferRequest
{
    [Required] public string ReceiverPhone { get; set; } = string.Empty;
    [Required, Range(1000, 100_000_000)] public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
