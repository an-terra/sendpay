using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Auth;
using SendPay.Api.Infrastructure;
using SendPay.Api.Models;
using SendPay.Api.Security;

namespace SendPay.Api.Services;

public class AuthService(
    AppDbContext db,
    IConfiguration config,
    IWebHostEnvironment env,
    IRefreshTokenService refreshTokens,
    IAuditService audit,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    string? ClientIp => httpContextAccessor.HttpContext?.GetClientIpAddress();

    int AccessMinutes => config.GetValue("Jwt:AccessTokenMinutes", 2880);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        PasswordPolicy.EnsureStrongOrThrow(req.Password);

        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            throw AppError.BadRequest(ErrorCodes.EmailInUse, "Email đã được sử dụng.");

        if (await db.Users.AnyAsync(u => u.Phone == req.Phone))
            throw AppError.BadRequest(ErrorCodes.PhoneInUse, "Số điện thoại đã được sử dụng.");

        var user = new User
        {
            FullName     = req.FullName,
            Email        = req.Email,
            Phone        = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (jwt, expUtc, _) = CreateAccessToken(user);
        var (refreshPlain, _) = await refreshTokens.IssueAsync(user.Id, ClientIp);

        await audit.WriteAsync("auth.register", $"email={user.Email}", user.Id, ClientIp);

        return new AuthResponse
        {
            UserId                   = user.Id,
            Token                    = jwt,
            RefreshToken             = refreshPlain,
            AccessTokenExpiresAtUtc  = expUtc,
            FullName                 = user.FullName,
            Email                    = user.Email,
            Phone                    = user.Phone,
            IsAdmin                  = user.IsAdmin
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email)
            ?? throw AppError.Unauthorized(ErrorCodes.AuthInvalidCredentials, "Email hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            await audit.WriteAsync("auth.login_failed", $"email={req.Email}", null, ClientIp);
            throw AppError.Unauthorized(ErrorCodes.AuthInvalidCredentials, "Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
            throw AppError.Unauthorized(ErrorCodes.AuthAccountLocked, "Tài khoản đã bị khóa.");

        await refreshTokens.RevokeAllForUserAsync(user.Id);

        var (jwt, expUtc, _) = CreateAccessToken(user);
        var (refreshPlain, _) = await refreshTokens.IssueAsync(user.Id, ClientIp);

        await audit.WriteAsync("auth.login", $"userId={user.Id}", user.Id, ClientIp);

        return new AuthResponse
        {
            UserId                   = user.Id,
            Token                    = jwt,
            RefreshToken             = refreshPlain,
            AccessTokenExpiresAtUtc  = expUtc,
            FullName                 = user.FullName,
            Email                    = user.Email,
            Phone                    = user.Phone,
            IsAdmin                  = user.IsAdmin
        };
    }

    public async Task<AuthResponse> RefreshTokensAsync(RefreshRequest req)
    {
        var result = await refreshTokens.ValidateAndRotateAsync(req.RefreshToken, ClientIp);
        if (result is null)
            throw AppError.Unauthorized(ErrorCodes.AuthRefreshInvalid, "Refresh token không hợp lệ hoặc đã hết hạn.");

        var (user, newRefresh) = result.Value;
        var (jwt, expUtc, _) = CreateAccessToken(user);

        await audit.WriteAsync("auth.refresh", $"userId={user.Id}", user.Id, ClientIp);

        return new AuthResponse
        {
            UserId                   = user.Id,
            Token                    = jwt,
            RefreshToken             = newRefresh,
            AccessTokenExpiresAtUtc  = expUtc,
            FullName                 = user.FullName,
            Email                    = user.Email,
            Phone                    = user.Phone,
            IsAdmin                  = user.IsAdmin
        };
    }

    public async Task LogoutAsync(int userId, string? accessJti, DateTime? accessExpiresUtc)
    {
        await refreshTokens.RevokeAllForUserAsync(userId);

        if (!string.IsNullOrEmpty(accessJti) && accessExpiresUtc.HasValue && accessExpiresUtc.Value > DateTime.UtcNow)
        {
            if (!await db.JwtBlacklistEntries.AnyAsync(x => x.Jti == accessJti))
            {
                db.JwtBlacklistEntries.Add(new JwtBlacklistEntry
                {
                    Jti          = accessJti,
                    UserId       = userId,
                    ExpiresAtUtc = accessExpiresUtc.Value
                });
                await db.SaveChangesAsync();
            }
        }

        await audit.WriteAsync("auth.logout", $"userId={userId}", userId, ClientIp);
        logger.LogInformation("User {UserId} logged out.", userId);
    }

    (string jwt, DateTime expiresAtUtc, string jti) CreateAccessToken(User user)
    {
        var jwtKey = JwtKeyResolver.ResolveSigningKey(config, env);
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti    = Guid.NewGuid().ToString("N");

        var claimsList = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };
        if (user.IsAdmin)
            claimsList.Add(new Claim(ClaimTypes.Role, "Admin"));

        var exp = DateTime.UtcNow.AddMinutes(AccessMinutes);
        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claimsList,
            expires:            exp,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), exp, jti);
    }
}
