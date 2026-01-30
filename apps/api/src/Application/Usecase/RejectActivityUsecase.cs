using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class RejectActivityUsecase : IUsecase<RejectActivityRequest, Activity>
{
    private readonly IActivityRepository _activities;
    private readonly IAffiliationRepository _affiliations;
    private readonly IAuthenticator _authenticator;

    public RejectActivityUsecase(IActivityRepository activities, IAffiliationRepository affiliations, IAuthenticator authenticator)
    {
        _activities = activities;
        _affiliations = affiliations;
        _authenticator = authenticator;
    }

    public async Task<Activity> ExecuteAsync(RejectActivityRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.ActivityId))
        {
            throw new ArgumentException("activity_id is required");
        }
        if (string.IsNullOrWhiteSpace(request.AffiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }

        var activity = await _activities.GetActivityAsync(request.ActivityId, cancellationToken);
        if (activity == null)
        {
            throw new KeyNotFoundException("activity_not_found");
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

        var updated = await _activities.UpdateActivityStatusAsync(activity, ActivityStatus.Redacted, hideFromTimeline: true, cancellationToken);
        return new Activity
        {
            Id = updated.Id,
            AffiliationId = updated.AffiliationId,
            WorldId = updated.WorldId,
            OwnerId = updated.OwnerId,
            Content = updated.Content,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            ExpiresAt = updated.ExpiresAt,
            CoCreatorIds = updated.CoCreators,
            SignatureIds = updated.Signatures
        };
    }
}
