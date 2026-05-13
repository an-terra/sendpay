using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.User;

namespace SendPay.Api.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<UserProfileResponse> GetProfileAsync(int userId)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserProfileResponse(u.Id, u.FullName, u.Email, u.Phone, u.Balance, u.CreatedAt);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (await db.Users.AnyAsync(x => x.Email == req.Email && x.Id != userId))
            throw new InvalidOperationException("Email đã được sử dụng.");

        if (await db.Users.AnyAsync(x => x.Phone == req.Phone && x.Id != userId))
            throw new InvalidOperationException("Số điện thoại đã được sử dụng.");

        u.FullName = req.FullName;
        u.Email    = req.Email;
        u.Phone    = req.Phone;
        await db.SaveChangesAsync();
        return new UserProfileResponse(u.Id, u.FullName, u.Email, u.Phone, u.Balance, u.CreatedAt);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var u = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, u.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");

        if (req.NewPassword.Length < 6)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự.");

        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
    }
}
