using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class PostActivityUsecase : IUsecase<PostActivityRequest, Activity>
{
    private readonly IActivityRepository _repository;
    private readonly IAffiliationRepository _affiliations;
    private readonly IAuthenticator _authenticator;

    public PostActivityUsecase(IActivityRepository repository, IAffiliationRepository affiliations, IAuthenticator authenticator)
    {
        _repository = repository;
        _affiliations = affiliations;
        _authenticator = authenticator;
    }

    public async Task<Activity> ExecuteAsync(PostActivityRequest request, CancellationToken cancellationToken)
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
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("content is required");
        }

        var affiliation = await _affiliations.GetAffiliationAsync(request.AffiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }
        if (!string.Equals(affiliation.OwnerId, auth.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }
        if (affiliation.Status != AffiliationStatus.Active)
        {
            throw new UnauthorizedAccessException("affiliation_not_active");
        }

        var now = DateTime.UtcNow.ToString("O");
        var normalizedCoCreators = (request.CoCreators ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var requiredSignatures = new HashSet<string>(StringComparer.Ordinal) { request.AffiliationId };
        foreach (var id in normalizedCoCreators)
        {
            requiredSignatures.Add(id);
        }
        var signatures = new List<string> { request.AffiliationId };
        var isMultiSig = normalizedCoCreators.Count > 0;
        var status = isMultiSig ? ActivityStatus.PendingMultiSig : ActivityStatus.Published;

        var activity = new Activity
        {
            Id = Guid.NewGuid().ToString("N"),
            AffiliationId = request.AffiliationId,
            WorldId = affiliation.WorldId,
            OwnerId = auth.UserId,
            Content = request.Content.Trim(),
            Status = status,
            CreatedAt = now,
            CoCreatorIds = normalizedCoCreators,
            SignatureIds = signatures
        };

        return await _repository.CreateActivityAsync(activity, requiredSignatures.ToList(), signatures, !isMultiSig, cancellationToken);
    }
}
