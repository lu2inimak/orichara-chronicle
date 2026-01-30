using Api.Modules.Activity.Models;

namespace Api.Modules.Activity.Store;

public interface IActivityStore
{
    Task<AffiliationRecord?> GetAffiliationAsync(string affiliationId, CancellationToken cancellationToken);
    Task<Models.Activity> CreateActivityAsync(Models.Activity activity, List<string> requiredSignatures, List<string> signatures, bool publishTimeline, CancellationToken cancellationToken);
    Task<ActivityRecord?> GetActivityAsync(string activityId, CancellationToken cancellationToken);
    Task<ActivityRecord> UpdateActivitySignaturesAsync(ActivityRecord record, List<string> signatures, string status, CancellationToken cancellationToken);
    Task PublishActivityAsync(ActivityRecord record, CancellationToken cancellationToken);
    Task<IReadOnlyList<Models.Activity>> ListWorldTimelineAsync(string worldId, int limit, CancellationToken cancellationToken);
}

public sealed class AffiliationRecord
{
    public string Id { get; init; } = string.Empty;
    public string WorldId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class ActivityRecord
{
    public string Id { get; init; } = string.Empty;
    public string AffiliationId { get; init; } = string.Empty;
    public string WorldId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public List<string> RequiredSignatures { get; init; } = new();
    public List<string> Signatures { get; init; } = new();
    public List<string> CoCreators { get; init; } = new();
}
