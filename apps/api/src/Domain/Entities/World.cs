namespace Api.Domain.Entities;

public sealed class World : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string HostId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
