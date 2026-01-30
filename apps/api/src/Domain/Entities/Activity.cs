using Api.Domain.Enums;

namespace Api.Domain.Entities;

public sealed class Activity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string AffiliationId { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ActivityStatus Status { get; set; } = ActivityStatus.Published;
    public string CreatedAt { get; set; } = string.Empty;
    public string? ExpiresAt { get; set; }
    public List<string> CoCreatorIds { get; set; } = new();
    public List<string> SignatureIds { get; set; } = new();
}
