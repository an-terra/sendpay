using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.DTOs.Wallet;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController(IWalletService walletService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetBalance()
    {
        var result = await walletService.GetBalanceAsync(UserId);
        return Ok(result);
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp(TopUpRequest request)
        => Ok(await walletService.TopUpAsync(UserId, request));
}
