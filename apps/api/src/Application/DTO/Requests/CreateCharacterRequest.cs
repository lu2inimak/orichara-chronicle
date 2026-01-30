namespace Api.Application.DTO;

public sealed record CreateCharacterRequest(
    string? AuthToken,
    string Name,
    string? Bio,
    string? AvatarUrl
);
