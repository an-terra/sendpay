namespace SendPay.Api.Services;

public record VerificationStartResult(
    Guid VerificationId,
    int ExpiresInSeconds,
    string? DebugOtp,
    string? DeliveryMessage);

public interface IOtpVerificationService
{
    Task<VerificationStartResult> StartTopUpAsync(int userId, decimal amount);
    Task<VerificationStartResult> StartTransferAsync(int userId, string receiverPhone, decimal amount, string? note);

    Task VerifyTopUpAsync(int userId, Guid verificationId, string code, decimal amount);
    Task VerifyTransferAsync(int userId, Guid verificationId, string code, string receiverPhone, decimal amount, string? note);
}
