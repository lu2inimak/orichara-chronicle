using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class ApproveAffiliationUsecase : IUsecase<ApproveAffiliationRequest, Affiliation>
{
    private readonly IWorldRepository _repository;
    private readonly IAffiliationRepository _affiliations;
    private readonly IAuthenticator _authenticator;

    public ApproveAffiliationUsecase(IWorldRepository repository, IAffiliationRepository affiliations, IAuthenticator authenticator)
    {
        _repository = repository;
        _affiliations = affiliations;
        _authenticator = authenticator;
    }

    public async Task<Affiliation> ExecuteAsync(ApproveAffiliationRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.AffiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }

        var affiliation = await _affiliations.GetAffiliationAsync(request.AffiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }

        var world = await _repository.GetWorldAsync(affiliation.WorldId, cancellationToken);
        if (world == null)
        {
            throw new KeyNotFoundException("world_not_found");
        }
        if (!string.Equals(world.HostId, auth.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("forbidden");
        }

        return await _affiliations.UpdateAffiliationStatusAsync(affiliation, AffiliationStatus.Active, cancellationToken);
    }
}
