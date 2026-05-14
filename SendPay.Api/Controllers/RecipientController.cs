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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try { return Ok(await svc.GetByIdAsync(UserId, id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Add(RecipientRequest req)
    {
        try { return Ok(await svc.AddAsync(UserId, req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RecipientRequest req)
    {
        try { return Ok(await svc.UpdateAsync(UserId, id, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await svc.DeleteAsync(UserId, id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
