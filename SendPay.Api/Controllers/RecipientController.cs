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
    public async Task<IActionResult> GetById(int id) => Ok(await svc.GetByIdAsync(UserId, id));

    [HttpPost]
    public async Task<IActionResult> Add(RecipientRequest req) => Ok(await svc.AddAsync(UserId, req));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RecipientRequest req)
        => Ok(await svc.UpdateAsync(UserId, id, req));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await svc.DeleteAsync(UserId, id);
        return NoContent();
    }
}
