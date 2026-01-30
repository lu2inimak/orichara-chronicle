using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.ReadModels;
using Api.Domain.Repositories;
using System.Globalization;

namespace Api.Application.Usecase;

public sealed class ArchivePendingActivityUsecase : IUsecase<ArchivePendingActivityRequest, Activity>
{
    private readonly IActivityRepository _activities;
    private readonly IAuthenticator _authenticator;

    public ArchivePendingActivityUsecase(IActivityRepository activities, IAuthenticator authenticator)
    {
        _activities = activities;
        _authenticator = authenticator;
    }

    public async Task<Activity> ExecuteAsync(ArchivePendingActivityRequest request, CancellationToken cancellationToken)
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

        var activity = await _activities.GetActivityAsync(request.ActivityId, cancellationToken);
        if (activity == null)
        {
            throw new KeyNotFoundException("activity_not_found");
        }

        if (activity.Status != ActivityStatus.PendingMultiSig)
        {
            return Map(activity);
        }

        if (string.IsNullOrWhiteSpace(activity.ExpiresAt))
        {
            return Map(activity);
        }

        if (!DateTimeOffset.TryParse(activity.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiresAt))
        {
            return Map(activity);
        }

        if (DateTimeOffset.UtcNow <= expiresAt)
        {
            return Map(activity);
        }

        var updated = await _activities.UpdateActivityStatusAsync(activity, ActivityStatus.ArchivedPending, hideFromTimeline: true, cancellationToken);
        return Map(updated);
    }

    private static Activity Map(ActivityRecord record)
    {
        return new Activity
        {
            Id = record.Id,
            AffiliationId = record.AffiliationId,
            WorldId = record.WorldId,
            OwnerId = record.OwnerId,
            Content = record.Content,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt,
            CoCreatorIds = record.CoCreators,
            SignatureIds = record.Signatures
        };
    }
}
