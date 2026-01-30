using Api.Domain.Entities;

namespace Api.Application.DTO;

public sealed class MeResponse
{
    public UserProfile User { get; init; } = new();
    public IReadOnlyList<string> OwnedCharacterIds { get; init; } = new List<string>();
    public IReadOnlyList<string> HostedWorldIds { get; init; } = new List<string>();
}
