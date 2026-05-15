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
        => Ok(await transactionService.TransferAsync(UserId, request));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await transactionService.GetByIdAsync(UserId, id));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await transactionService.GetHistoryAsync(UserId, page, pageSize));
}
