namespace Api.Application.DTO;

public sealed record PostActivityRequest(
    string? AuthToken,
    string AffiliationId,
    string Content,
    List<string>? CoCreators
);
