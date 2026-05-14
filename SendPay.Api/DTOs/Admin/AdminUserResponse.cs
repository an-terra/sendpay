namespace SendPay.Api.DTOs.Admin;

public record AdminUserResponse(
    int Id, string FullName, string Email, string Phone,
    decimal Balance, bool IsActive, bool IsAdmin, DateTime CreatedAt,
    string? JapanBankName, string? JapanBankTopUpUrl);
