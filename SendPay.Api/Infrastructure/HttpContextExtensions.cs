namespace SendPay.Api.Infrastructure;

public static class HttpContextExtensions
{
    /// <summary>
    /// IP thực (Render/proxy thường set X-Forwarded-For).
    /// </summary>
    public static string GetClientIpAddress(this HttpContext context)
    {
        var fwd = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
