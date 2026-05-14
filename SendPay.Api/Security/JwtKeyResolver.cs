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

        if (jwtKey.Length < 32)
            throw new InvalidOperationException("Jwt:Key phải dài ít nhất 32 ký tự.");

        return jwtKey;
    }
}
