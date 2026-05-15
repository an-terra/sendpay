namespace SendPay.Api.Infrastructure;

/// <summary>
/// Exception nghi\u1ec7p v\u1ee5 chu\u1ea9n c\u1ee7a SendPay. Mang theo:
///   - <see cref="Code"/>: kh\u00f3a d\u1ecbch \u1edf frontend (xem <see cref="ErrorCodes"/>).
///   - <see cref="Args"/>: tham s\u1ed1 \u0111\u1ec3 th\u1ebf v\u00e0o th\u00f4ng \u0111i\u1ec7p ({key}).
///   - <see cref="StatusCode"/>: HTTP status muốn tr\u1ea3 v\u1ec1.
/// Th\u00f4ng \u0111i\u1ec7p (<c>Message</c>) ch\u1ec9 l\u00e0 fallback ti\u1ebfng Vi\u1ec7t khi frontend kh\u00f4ng c\u00f3 b\u1ea3n d\u1ecbch.
/// </summary>
public sealed class AppError : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, object?> Args { get; }

    public AppError(string code, string fallbackMessage, int statusCode = 400, IReadOnlyDictionary<string, object?>? args = null)
        : base(fallbackMessage)
    {
        Code       = code;
        StatusCode = statusCode;
        Args       = args ?? new Dictionary<string, object?>();
    }

    /// <summary>HTTP 400 (Bad Request).</summary>
    public static AppError BadRequest(string code, string fallback, object? args = null)
        => new(code, fallback, StatusCodes.Status400BadRequest, ArgsFrom(args));

    /// <summary>HTTP 401 (Unauthorized).</summary>
    public static AppError Unauthorized(string code, string fallback, object? args = null)
        => new(code, fallback, StatusCodes.Status401Unauthorized, ArgsFrom(args));

    /// <summary>HTTP 403 (Forbidden).</summary>
    public static AppError Forbidden(string code, string fallback, object? args = null)
        => new(code, fallback, StatusCodes.Status403Forbidden, ArgsFrom(args));

    /// <summary>HTTP 404 (Not Found).</summary>
    public static AppError NotFound(string code, string fallback, object? args = null)
        => new(code, fallback, StatusCodes.Status404NotFound, ArgsFrom(args));

    /// <summary>HTTP 409 (Conflict).</summary>
    public static AppError Conflict(string code, string fallback, object? args = null)
        => new(code, fallback, StatusCodes.Status409Conflict, ArgsFrom(args));

    private static IReadOnlyDictionary<string, object?> ArgsFrom(object? raw)
    {
        if (raw is null) return new Dictionary<string, object?>();
        if (raw is IReadOnlyDictionary<string, object?> ro) return ro;
        if (raw is IDictionary<string, object?> dict) return new Dictionary<string, object?>(dict);

        var result = new Dictionary<string, object?>();
        foreach (var prop in raw.GetType().GetProperties())
            result[prop.Name] = prop.GetValue(raw);
        return result;
    }
}
