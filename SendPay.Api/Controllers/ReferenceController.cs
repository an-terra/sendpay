using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.Data;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceController : ControllerBase
{
    [HttpGet("vietnam-banks")]
    public IActionResult VietnamBanks() =>
        Ok(VietnamBankCatalog.All.Select(e => new { name = e.Name, swift = e.Swift }));
}
