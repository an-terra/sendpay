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

    /// <summary>Autocomplete ngân hàng Nhật. q là từ khoá (mã, kana, kanji, en, alias).</summary>
    [HttpGet("japan-banks")]
    public IActionResult JapanBanks([FromQuery] string? q = null, [FromQuery] int max = 25)
    {
        var capped = Math.Clamp(max, 1, 50);
        return Ok(JapanBankCatalog.Search(q, capped)
            .Select(e => new
            {
                code = e.Code,
                nameJa = e.NameJa,
                nameEn = e.NameEn,
                emoji = e.Emoji
            }));
    }
}
