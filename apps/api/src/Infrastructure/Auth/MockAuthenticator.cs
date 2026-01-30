using Api.Application.Auth;
using Api.Application.Common;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Auth;

public sealed class MockAuthenticator : IAuthenticator
{
    private readonly ILogger<MockAuthenticator> _logger;

    public MockAuthenticator(ILogger<MockAuthenticator> logger)
    {
        _logger = logger;
    }

    public AuthInfo? Authenticate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("event=auth.check request_id={RequestId} success=false reason=missing_token", RequestContext.RequestId);
            return null;
        }

        var userId = token;
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            userId = token[7..].Trim();
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogInformation("event=auth.check request_id={RequestId} success=false reason=empty_user", RequestContext.RequestId);
            return null;
        }

        _logger.LogInformation("event=auth.check request_id={RequestId} success=true user_id={UserId}", RequestContext.RequestId, userId);
        return new AuthInfo(userId);
    }
}
