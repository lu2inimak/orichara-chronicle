namespace Api.Application.DTO;

public sealed record UpdateCharacterRequest(
    string? AuthToken,
    string CharacterId,
    Dictionary<string, string> Updates
);
