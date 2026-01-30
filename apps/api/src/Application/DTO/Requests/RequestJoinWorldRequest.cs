namespace Api.Application.DTO;

public sealed record RequestJoinWorldRequest(
    string? AuthToken,
    string WorldId,
    string CharacterId
);
