namespace Api.Application.DTO;

public sealed record ArchivePendingActivityRequest(
    string? AuthToken,
    string ActivityId
);
