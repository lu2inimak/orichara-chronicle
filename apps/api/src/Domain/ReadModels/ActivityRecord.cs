using Api.Domain.Enums;

namespace Api.Domain.ReadModels;

public sealed class ActivityRecord
{
    public string Id { get; init; } = string.Empty;
    public string AffiliationId { get; init; } = string.Empty;
    public string WorldId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public ActivityStatus Status { get; init; } = ActivityStatus.Published;
    public string CreatedAt { get; init; } = string.Empty;
    public string? ExpiresAt { get; init; }
    public List<string> RequiredSignatures { get; init; } = new();
    public List<string> Signatures { get; init; } = new();
    public List<string> CoCreators { get; init; } = new();
}
