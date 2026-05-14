using System.ComponentModel.DataAnnotations;

namespace SendPay.Api.DTOs.Auth;

public class RegisterRequest
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, Phone] public string Phone { get; set; } = string.Empty;
    /// <summary>Được kiểm tra bởi PasswordPolicy (API khớp chuẩn Blazor Web).</summary>
    [Required] public string Password { get; set; } = string.Empty;
}
