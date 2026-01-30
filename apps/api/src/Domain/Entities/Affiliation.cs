using Api.Domain.Enums;

namespace Api.Domain.Entities;

public sealed class Affiliation : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public AffiliationStatus Status { get; set; } = AffiliationStatus.Pending;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
