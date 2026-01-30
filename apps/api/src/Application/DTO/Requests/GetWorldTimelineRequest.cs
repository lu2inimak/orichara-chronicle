namespace Api.Application.DTO;

public sealed record GetWorldTimelineRequest(
    string? AuthToken,
    string WorldId,
    int Limit
);
