using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Admin;

public class BankStatementImportRequest
{
    [Required]
    public List<BankStatementLineItem> Lines { get; set; } = [];
}

public class BankStatementLineItem
{
    [Required]
    public DateTime BookingDate { get; set; }

    [Range(0.01, 100_000_000)]
    public decimal Amount { get; set; }

    [Required, MaxLength(4000)]
    public string Memo { get; set; } = "";

    [MaxLength(64)]
    public string? CreditAccountNumber { get; set; }

    [MaxLength(64)]
    public string? Source { get; set; }
}
