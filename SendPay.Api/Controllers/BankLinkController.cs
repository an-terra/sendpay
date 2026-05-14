using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SendPay.Api.DTOs.BankLink;
using SendPay.Api.Infrastructure;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/bank-link")]
public class BankLinkController(IBankLinkService svc) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    [HttpPost("start")]
    [EnableRateLimiting("bank-link")]
    public async Task<IActionResult> Start([FromBody] BankLinkStartRequest req)
    {
        try
        {
            var ip = HttpContext.GetClientIpAddress();
            var data = await svc.StartAsync(UserId, req, ip);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("fake-approve")]
    [EnableRateLimiting("bank-link")]
    public async Task<IActionResult> FakeApprove([FromBody] FakeBankApproveRequest req)
    {
        try
        {
            var ip = HttpContext.GetClientIpAddress();
            var data = await svc.FakeApproveAsync(req, ip);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me() => Ok(await svc.GetMyLinksAsync(UserId));

    [Authorize]
    [HttpGet("primary")]
    public async Task<IActionResult> GetPrimary()
    {
        var p = await svc.GetPrimaryAsync(UserId);
        return p is null ? NoContent() : Ok(p);
    }

    [Authorize]
    [HttpDelete("{linkId:int}")]
    public async Task<IActionResult> Unlink(int linkId)
    {
        var ok = await svc.UnlinkAsync(UserId, linkId);
        return ok ? Ok(new { message = "Đã hủy liên kết." }) : NotFound();
    }

    [Authorize]
    [HttpPut("{linkId:int}/primary")]
    public async Task<IActionResult> SetPrimary(int linkId)
    {
        var ok = await svc.SetPrimaryAsync(UserId, linkId);
        return ok ? Ok(new { message = "Đã đặt ngân hàng chính." }) : NotFound();
    }
}
