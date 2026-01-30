namespace Api.Modules.User.Models;

public sealed class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public sealed class MeResponse
{
    public UserProfile User { get; init; } = new();
    public List<string> OwnedCharacterIds { get; init; } = new();
    public List<string> HostedWorldIds { get; init; } = new();
}
