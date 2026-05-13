namespace SendPay.Api.DTOs.Admin;

public record AdminUpdateUserRequest(
    string FullName,
    string Email,
    string Phone,
    decimal? Balance);
