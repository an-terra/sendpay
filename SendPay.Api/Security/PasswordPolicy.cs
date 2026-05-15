using SendPay.Api.Infrastructure;

namespace SendPay.Api.Security;

/// <summary>
/// Cùng chuẩn với SendPay.Web (Register.razor / Settings.razor).
/// </summary>
public static class PasswordPolicy
{
    public const string RequirementDescriptionVi =
        "Mật khẩu phải có ít nhất 8 ký tự, gồm ít nhất 1 chữ hoa và 1 ký tự đặc biệt.";

    public static bool IsStrongPassword(string? password) =>
        !string.IsNullOrEmpty(password) &&
        password.Length >= 8 &&
        password.Any(char.IsUpper) &&
        password.Any(c => !char.IsLetterOrDigit(c));

    public static void EnsureStrongOrThrow(string password)
    {
        if (!IsStrongPassword(password))
            throw AppError.BadRequest(ErrorCodes.PasswordWeak, RequirementDescriptionVi);
    }
}
