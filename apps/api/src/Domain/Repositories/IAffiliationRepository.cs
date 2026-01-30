using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Api.Domain.Repositories;

public interface IAffiliationRepository
{
    Task<Affiliation> CreateAffiliationAsync(Affiliation affiliation, CancellationToken cancellationToken);
    Task<Affiliation?> GetAffiliationAsync(string affiliationId, CancellationToken cancellationToken);
    Task<Affiliation> UpdateAffiliationStatusAsync(Affiliation affiliation, AffiliationStatus status, CancellationToken cancellationToken);
}
