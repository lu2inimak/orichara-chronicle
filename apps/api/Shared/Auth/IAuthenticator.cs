namespace Api.Shared.Auth;

public interface IAuthenticator
{
    AuthInfo? Authenticate(HttpContext context);
}
