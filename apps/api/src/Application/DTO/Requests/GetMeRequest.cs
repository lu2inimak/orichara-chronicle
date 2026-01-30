namespace Api.Application.DTO;

public sealed record GetMeRequest(
    string? AuthToken
);
