namespace Api.Shared.Auth;

public sealed class MockAuthenticator : IAuthenticator
{
    public AuthInfo? Authenticate(HttpContext context)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            var auth = context.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                userId = auth[7..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return new AuthInfo(userId);
    }
}
