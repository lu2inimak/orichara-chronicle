namespace Api.Application.DTO;

public sealed record SignActivityRequest(
    string? AuthToken,
    string ActivityId,
    string AffiliationId
);
