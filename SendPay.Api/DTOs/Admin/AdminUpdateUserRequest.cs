namespace SendPay.Api.DTOs.Admin;

public record AdminUpdateUserRequest(
    string FullName,
    string Email,
    string Phone,
    decimal? Balance,
    bool IsActive,
    bool IsAdmin,
    string? JapanBankName,
    string? JapanBankTopUpUrl,
    /// <summary>Để trống = không đổi mật khẩu.</summary>
    string? NewPassword);
