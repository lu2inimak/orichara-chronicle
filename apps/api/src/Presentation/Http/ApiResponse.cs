namespace Api.Presentation.Http;

using Microsoft.AspNetCore.Http;

public sealed class ApiResponse<T>
{
    public bool Ok { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public object Meta { get; init; } = new { version = "v1" };
    public string RequestId { get; init; } = string.Empty;
}

public static class ApiResults
{
    public static IResult Ok<T>(HttpContext context, T data, int status = 200)
    {
        var requestId = RequestId(context);
        var payload = new ApiResponse<T>
        {
            Ok = true,
            Data = data,
            RequestId = requestId
        };
        return Results.Json(payload, statusCode: status);
    }

    public static IResult Error(HttpContext context, ApiError error)
    {
        var requestId = RequestId(context);
        var payload = new ApiResponse<object>
        {
            Ok = false,
            Error = error,
            RequestId = requestId
        };
        return Results.Json(payload, statusCode: error.Status);
    }

    public static string RequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Request-Id", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }
        if (context.Request.Headers.TryGetValue("X-Request-ID", out var alt) && !string.IsNullOrWhiteSpace(alt))
        {
            return alt.ToString();
        }
        return "req_" + Guid.NewGuid().ToString("N");
    }
}
