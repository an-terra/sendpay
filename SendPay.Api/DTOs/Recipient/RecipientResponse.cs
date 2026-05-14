namespace SendPay.Api.DTOs.Recipient;

public record RecipientResponse(
    int Id,
    string Name,
    string? Phone,
    string Note,
    string? CountryCode,
    string? BankName,
    string? AccountNumber,
    string? AccountHolderName,
    string? SwiftBic,
    DateTime CreatedAt);
