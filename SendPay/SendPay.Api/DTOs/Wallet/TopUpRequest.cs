using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Wallet;

public class TopUpRequest
{
    [Required, Range(10000, 100_000_000)]
    public decimal Amount { get; set; }
}
