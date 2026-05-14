namespace SendPay.Api.DTOs.Recipient;

public record RecipientRequest(
    string Name,
    string? Phone,
    string Note = "",
    string? CountryCode = null,
    string? BankName = null,
    string? AccountNumber = null,
    string? AccountHolderName = null);
