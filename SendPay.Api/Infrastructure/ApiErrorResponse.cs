using Microsoft.AspNetCore.Mvc;

namespace SendPay.Api.Infrastructure;

/// <summary>
/// Payload chu\u1ea9n cho m\u1ecdi l\u1ed7i tr\u1ea3 v\u1ec1 t\u1eeb backend:
///   { "code": "error.xxx", "message": "fallback vi\u1ec7t", "args": { ... } }
/// Frontend l\u1ea5y <c>code</c> + <c>args</c> \u0111\u1ec3 hi\u1ec3n th\u1ecb b\u1eb1ng ng\u00f4n ng\u1eef \u0111ang d\u00f9ng.
/// </summary>
public sealed class ApiErrorResponse
{
    public string Code { get; init; } = ErrorCodes.System;
    public string Message { get; init; } = "";
    public IReadOnlyDictionary<string, object?>? Args { get; init; }

    public static ApiErrorResponse From(AppError e) => new()
    {
        Code    = e.Code,
        Message = e.Message,
        Args    = e.Args.Count == 0 ? null : e.Args
    };

    public static ApiErrorResponse System(string fallback) => new()
    {
        Code    = ErrorCodes.System,
        Message = fallback,
    };
}

public static class AppErrorActionResults
{
    public static IActionResult ToActionResult(this AppError e)
        => new ObjectResult(ApiErrorResponse.From(e)) { StatusCode = e.StatusCode };
}
