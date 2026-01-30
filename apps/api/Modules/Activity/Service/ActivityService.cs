using Api.Modules.Activity.Models;
using Api.Modules.Activity.Store;

namespace Api.Modules.Activity.Service;

public sealed class ActivityService
{
    private readonly IActivityStore _store;

    public ActivityService(IActivityStore store)
    {
        _store = store;
    }

    public async Task<Models.Activity> PostActivityAsync(string userId, string affiliationId, string content, List<string> coCreators, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(affiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("content is required");
        }

        var affiliation = await _store.GetAffiliationAsync(affiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }
        if (!string.Equals(affiliation.OwnerId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }
        if (!string.Equals(affiliation.Status, "Active", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("affiliation_not_active");
        }

        var now = DateTime.UtcNow.ToString("O");
        var normalizedCoCreators = coCreators
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var requiredSignatures = new HashSet<string>(StringComparer.Ordinal) { affiliationId };
        foreach (var id in normalizedCoCreators)
        {
            requiredSignatures.Add(id);
        }
        var signatures = new List<string> { affiliationId };
        var isMultiSig = normalizedCoCreators.Count > 0;
        var status = isMultiSig ? ActivityStatuses.PendingMultiSig : ActivityStatuses.Published;

        var activity = new Models.Activity
        {
            Id = Guid.NewGuid().ToString("N"),
            AffiliationId = affiliationId,
            WorldId = affiliation.WorldId,
            OwnerId = userId,
            Content = content.Trim(),
            Status = status,
            CreatedAt = now
        };
        activity.CoCreatorIds = normalizedCoCreators;
        activity.SignatureIds = signatures;

        return await _store.CreateActivityAsync(activity, requiredSignatures.ToList(), signatures, !isMultiSig, cancellationToken);
    }

    public Task<IReadOnlyList<Models.Activity>> GetTimelineAsync(string worldId, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(worldId))
        {
            throw new ArgumentException("world id is required");
        }
        return _store.ListWorldTimelineAsync(worldId, limit, cancellationToken);
    }

    public async Task<Models.Activity> SignActivityAsync(string userId, string activityId, string affiliationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(activityId))
        {
            throw new ArgumentException("activity_id is required");
        }
        if (string.IsNullOrWhiteSpace(affiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }

        var activity = await _store.GetActivityAsync(activityId, cancellationToken);
        if (activity == null)
        {
            throw new KeyNotFoundException("activity_not_found");
        }

        if (!activity.RequiredSignatures.Contains(affiliationId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("signer_not_in_cocreators");
        }
        if (activity.Signatures.Contains(affiliationId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("already_signed");
        }

        var affiliation = await _store.GetAffiliationAsync(affiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }
        if (!string.Equals(affiliation.OwnerId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        var newSignatures = new List<string>(activity.Signatures) { affiliationId };
        var allSigned = activity.RequiredSignatures.All(req => newSignatures.Contains(req, StringComparer.Ordinal));
        var newStatus = allSigned ? ActivityStatuses.Published : activity.Status;

        var updated = await _store.UpdateActivitySignaturesAsync(activity, newSignatures, newStatus, cancellationToken);
        if (allSigned)
        {
            await _store.PublishActivityAsync(updated, cancellationToken);
        }

        return new Models.Activity
        {
            Id = updated.Id,
            AffiliationId = updated.AffiliationId,
            WorldId = updated.WorldId,
            OwnerId = updated.OwnerId,
            Content = updated.Content,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            CoCreatorIds = updated.CoCreators,
            SignatureIds = updated.Signatures
        };
    }
}
