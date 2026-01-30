namespace Api.Domain.Entities;

public sealed class UserProfile : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}
