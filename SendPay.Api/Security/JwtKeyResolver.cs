namespace SendPay.Api.Security;

public static class JwtKeyResolver
{
    private const string DevFallbackKey = "DEV-ONLY-sendpay-jwt-key-min-32-chars!!";

    public static string ResolveSigningKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (environment.IsDevelopment())
                return DevFallbackKey;
            throw new InvalidOperationException(
                "Thiếu Jwt:Key. Trên Production hãy đặt biến môi trường Jwt__Key (>= 32 ký tự).");
        }

        var minLen = environment.IsDevelopment() ? 32 : 64;
        if (jwtKey.Length < minLen)
            throw new InvalidOperationException(
                environment.IsDevelopment()
                    ? "Jwt:Key phải dài ít nhất 32 ký tự (Development)."
                    : "Jwt:Key phải dài ít nhất 64 ký tự (Production). Dùng Jwt__Key trong secrets / env.");

        return jwtKey;
    }
}
