using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class GetMeUsecase : IUsecase<GetMeRequest, MeResponse>
{
    private readonly IUserRepository _repository;
    private readonly IAuthenticator _authenticator;

    public GetMeUsecase(IUserRepository repository, IAuthenticator authenticator)
    {
        _repository = repository;
        _authenticator = authenticator;
    }

    public async Task<MeResponse> ExecuteAsync(GetMeRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        var snapshot = await _repository.GetSnapshotAsync(auth.UserId, cancellationToken);
        return new MeResponse
        {
            User = snapshot.Profile,
            OwnedCharacterIds = snapshot.OwnedCharacterIds,
            HostedWorldIds = snapshot.HostedWorldIds
        };
    }
}
