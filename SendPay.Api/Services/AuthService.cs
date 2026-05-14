using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Auth;
using SendPay.Api.Models;
using SendPay.Api.Security;

namespace SendPay.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config, IWebHostEnvironment env) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        PasswordPolicy.EnsureStrongOrThrow(req.Password);

        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            throw new InvalidOperationException("Email đã được sử dụng.");

        if (await db.Users.AnyAsync(u => u.Phone == req.Phone))
            throw new InvalidOperationException("Số điện thoại đã được sử dụng.");

        var user = new User
        {
            FullName     = req.FullName,
            Email        = req.Email,
            Phone        = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResponse
        {
            UserId   = user.Id,
            Token    = GenerateToken(user),
            FullName = user.FullName,
            Email    = user.Email,
            Phone    = user.Phone,
            IsAdmin  = user.IsAdmin
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        return new AuthResponse
        {
            UserId   = user.Id,
            Token    = GenerateToken(user),
            FullName = user.FullName,
            Email    = user.Email,
            Phone    = user.Phone,
            IsAdmin  = user.IsAdmin
        };
    }

    private string GenerateToken(User user)
    {
        var jwtKey = JwtKeyResolver.ResolveSigningKey(config, env);
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(ClaimTypes.Email,          user.Email)
        };
        if (user.IsAdmin)
            claimsList.Add(new Claim(ClaimTypes.Role, "Admin"));

        var claims = claimsList.ToArray();

        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
