namespace SendPay.Api.DTOs.User;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
