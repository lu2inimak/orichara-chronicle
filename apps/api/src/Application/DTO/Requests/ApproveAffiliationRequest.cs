namespace Api.Application.DTO;

public sealed record ApproveAffiliationRequest(
    string? AuthToken,
    string AffiliationId
);
