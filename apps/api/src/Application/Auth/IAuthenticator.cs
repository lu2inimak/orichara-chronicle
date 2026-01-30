namespace Api.Application.Auth;

public interface IAuthenticator
{
    AuthInfo? Authenticate(string? token);
}
