namespace SendPay.Api.DTOs.User;

public record UserProfileResponse(
    int Id, string FullName, string Email, string Phone,
    decimal Balance, DateTime CreatedAt);
