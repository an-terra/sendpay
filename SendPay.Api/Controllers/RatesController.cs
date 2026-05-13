using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatesController(RatesService rates) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            var result = await rates.GetRatesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
