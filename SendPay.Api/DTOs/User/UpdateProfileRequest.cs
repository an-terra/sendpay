namespace SendPay.Api.DTOs.User;

public record UpdateProfileRequest(
    string FullName,
    string Email,
    string Phone,
    string? JapanBankName,
    string? JapanBankTopUpUrl);
