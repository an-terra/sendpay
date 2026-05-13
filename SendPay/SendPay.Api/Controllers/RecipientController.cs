using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.DTOs.Recipient;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipientController(IRecipientService svc) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await svc.GetAllAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> Add(RecipientRequest req) =>
        Ok(await svc.AddAsync(UserId, req));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await svc.DeleteAsync(UserId, id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
