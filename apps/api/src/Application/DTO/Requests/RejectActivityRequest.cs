namespace Api.Application.DTO;

public sealed record RejectActivityRequest(
    string? AuthToken,
    string ActivityId,
    string AffiliationId
);
