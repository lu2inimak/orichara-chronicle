using System.Threading;

namespace Api.Application.Common;

public static class RequestContext
{
    private static readonly AsyncLocal<string?> CurrentRequestId = new();

    public static string? RequestId
    {
        get => CurrentRequestId.Value;
        set => CurrentRequestId.Value = value;
    }
}
