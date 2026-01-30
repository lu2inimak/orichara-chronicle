using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class GetWorldTimelineUsecase : IUsecase<GetWorldTimelineRequest, IReadOnlyList<Activity>>
{
    private readonly IActivityRepository _repository;
    private readonly IAuthenticator _authenticator;

    public GetWorldTimelineUsecase(IActivityRepository repository, IAuthenticator authenticator)
    {
        _repository = repository;
        _authenticator = authenticator;
    }

    public Task<IReadOnlyList<Activity>> ExecuteAsync(GetWorldTimelineRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.WorldId))
        {
            throw new ArgumentException("world id is required");
        }
        return _repository.ListWorldTimelineAsync(request.WorldId, request.Limit, cancellationToken);
    }
}
