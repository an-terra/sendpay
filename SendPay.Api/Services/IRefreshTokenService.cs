using SendPay.Api.Models;

namespace SendPay.Api.Services;

public interface IRefreshTokenService
{
    Task<(string plainToken, UserRefreshToken entity)> IssueAsync(int userId, string? ipAddress, CancellationToken ct = default);
    Task<(User user, string plainRefresh)?> ValidateAndRotateAsync(string plainRefresh, string? ipAddress, CancellationToken ct = default);
    Task RevokeAllForUserAsync(int userId, CancellationToken ct = default);
    Task RevokeEntityAsync(UserRefreshToken token, CancellationToken ct = default);
}
