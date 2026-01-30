using Api.Domain.Entities;

namespace Api.Domain.ReadModels;

public sealed class UserSnapshot
{
    public UserProfile Profile { get; init; } = new();
    public IReadOnlyList<string> OwnedCharacterIds { get; init; } = new List<string>();
    public IReadOnlyList<string> HostedWorldIds { get; init; } = new List<string>();
}
