using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SendPay.Api.Services;

namespace SendPay.Api.Controllers;

[Authorize]
[EnableRateLimiting("otp")]
[ApiController]
[Route("api/[controller]")]
public class VerificationController(IOtpVerificationService otp, IWebHostEnvironment env) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost("topup/start")]
    public async Task<IActionResult> StartTopUp([FromBody] StartTopUpVerificationRequest body)
    {
        var r = await otp.StartTopUpAsync(UserId, body.Amount);
        return Ok(ToResponse(r));
    }

    [HttpPost("transfer/start")]
    public async Task<IActionResult> StartTransfer([FromBody] StartTransferVerificationRequest body)
    {
        var r = await otp.StartTransferAsync(UserId, body.ReceiverPhone, body.Amount, body.Note);
        return Ok(ToResponse(r));
    }

    private object ToResponse(VerificationStartResult r) =>
        env.IsDevelopment()
            ? new { verificationId = r.VerificationId, expiresInSeconds = r.ExpiresInSeconds, debugOtp = r.DebugOtp }
            : new
            {
                verificationId = r.VerificationId,
                expiresInSeconds = r.ExpiresInSeconds,
                message = "Mã xác thực đã được gửi. Kiểm tra SMS/email điện thoại đăng ký (cần tích hợp nhà cung cấp thật trên production)."
            };

    public class StartTopUpVerificationRequest
    {
        [Required, Range(1000, 100_000_000)] public decimal Amount { get; set; }
    }

    public class StartTransferVerificationRequest
    {
        [Required] public string ReceiverPhone { get; set; } = string.Empty;
        [Required, Range(1000, 100_000_000)] public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
