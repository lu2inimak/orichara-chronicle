using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.ReadModels;

namespace Api.Domain.Repositories;

public interface IActivityRepository
{
    Task<Activity> CreateActivityAsync(Activity activity, List<string> requiredSignatures, List<string> signatures, bool publishTimeline, CancellationToken cancellationToken);
    Task<ActivityRecord?> GetActivityAsync(string activityId, CancellationToken cancellationToken);
    Task<ActivityRecord> UpdateActivitySignaturesAsync(ActivityRecord record, List<string> signatures, ActivityStatus status, CancellationToken cancellationToken);
    Task PublishActivityAsync(ActivityRecord record, CancellationToken cancellationToken);
    Task<IReadOnlyList<Activity>> ListWorldTimelineAsync(string worldId, int limit, CancellationToken cancellationToken);
}
