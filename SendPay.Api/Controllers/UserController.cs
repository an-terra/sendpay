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
    {
        try { return Ok(await svc.UpdateProfileAsync(UserId, req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        try { await svc.ChangePasswordAsync(UserId, req); return Ok(new { message = "Đổi mật khẩu thành công." }); }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Danh bạ (saved) trước, sau user đăng ký. Hỗ trợ STK + tên NH.</summary>
    [HttpGet("receiver-lookup")]
    public async Task<IActionResult> LookupReceiver(
        [FromQuery] string? phone,
        [FromQuery] string? accountNumber,
        [FromQuery] string? bankName) =>
        Ok(await svc.LookupTransferCounterpartyAsync(UserId, phone, accountNumber, bankName));
}
