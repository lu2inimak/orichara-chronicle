using Api.Application.Auth;

namespace Api.Infrastructure.Auth;

public sealed class MockAuthenticator : IAuthenticator
{
    public AuthInfo? Authenticate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var userId = token;
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            userId = token[7..].Trim();
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return new AuthInfo(userId);
    }
}
