namespace SendPay.Api.Services;

public interface IAuditService
{
    Task WriteAsync(string action, string detail, int? userId, string? ipAddress, CancellationToken ct = default);
}
