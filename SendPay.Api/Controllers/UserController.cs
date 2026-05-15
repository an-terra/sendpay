using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.DTOs.User;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(IUserService svc) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile() =>
        Ok(await svc.GetProfileAsync(UserId));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
        => Ok(await svc.UpdateProfileAsync(UserId, req));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        await svc.ChangePasswordAsync(UserId, req);
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    /// <summary>Danh bạ (saved) trước, sau user đăng ký. Hỗ trợ STK + tên NH.</summary>
    [HttpGet("receiver-lookup")]
    public async Task<IActionResult> LookupReceiver(
        [FromQuery] string? phone,
        [FromQuery] string? accountNumber,
        [FromQuery] string? bankName) =>
        Ok(await svc.LookupTransferCounterpartyAsync(UserId, phone, accountNumber, bankName));
}
