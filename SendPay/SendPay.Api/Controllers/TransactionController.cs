using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendPay.Api.DTOs.Transaction;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferRequest request)
    {
        try
        {
            var result = await transactionService.TransferAsync(UserId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await transactionService.GetByIdAsync(UserId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await transactionService.GetHistoryAsync(UserId, page, pageSize);
        return Ok(result);
    }
}
