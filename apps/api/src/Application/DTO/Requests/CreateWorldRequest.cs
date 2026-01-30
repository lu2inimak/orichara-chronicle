namespace Api.Application.DTO;

public sealed record CreateWorldRequest(
    string? AuthToken,
    string Name,
    string? Description
);
