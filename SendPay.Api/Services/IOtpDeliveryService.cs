namespace SendPay.Api.Services;

/// <summary>Kết quả gửi thông báo OTP (email/SMS).</summary>
public sealed record OtpNotifyOutcome(string? ProductionMessage)
{
    /// <summary>Dev + chế độ Log: không gửi nhà cung cấp bên ngoài.</summary>
    public bool IsNoop { get; init; }
}

public interface IOtpDeliveryService
{
    Task<OtpNotifyOutcome> NotifyAsync(
        string email,
        string phone,
        string code,
        string actionDescription,
        CancellationToken cancellationToken = default);
}
