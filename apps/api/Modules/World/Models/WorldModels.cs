namespace Api.Modules.World.Models;

public sealed class World
{
    public string Id { get; set; } = string.Empty;
    public string HostId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class Affiliation
{
    public string Id { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public static class AffiliationStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
}
