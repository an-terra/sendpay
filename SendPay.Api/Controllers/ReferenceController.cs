using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.Data;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceController : ControllerBase
{
    /// <summary>Giữ tương thích client cũ.</summary>
    [HttpGet("vietnam-banks")]
    public IActionResult VietnamBanks() =>
        Ok(CountryBankCatalog.GetBanks("VN").Select(e => new { name = e.Name, swift = e.Swift }));

    /// <summary>Danh sách ngân hàng theo mã quốc gia (VN, LK, NP, PH).</summary>
    [HttpGet("banks/{country}")]
    public IActionResult BanksByCountry([FromRoute] string country)
    {
        var c = (country ?? "").Trim().ToUpperInvariant();
        if (!CountryBankCatalog.IsCatalogCountry(c))
            return NotFound();
        return Ok(CountryBankCatalog.GetBanks(c).Select(e => new { name = e.Name, swift = e.Swift }));
    }
}
