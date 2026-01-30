namespace Api.Shared.Http;

public sealed class ApiError
{
    public int Status { get; init; }
    public string Code { get; init; } = "INTERNAL_ERROR";
    public string Message { get; init; } = "Internal error.";
    public Dictionary<string, object>? Details { get; init; }
}
