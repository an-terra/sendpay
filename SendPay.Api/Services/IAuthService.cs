using SendPay.Api.DTOs.Auth;

namespace SendPay.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokensAsync(RefreshRequest request);
    Task LogoutAsync(int userId, string? accessJti, DateTime? accessExpiresUtc);
}
