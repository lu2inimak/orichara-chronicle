using Api.Modules.World.Models;

namespace Api.Modules.World.Store;

public interface IWorldStore
{
    Task<Models.World?> GetWorldAsync(string worldId, CancellationToken cancellationToken);
    Task<Models.World> CreateWorldAsync(string hostId, Models.World world, CancellationToken cancellationToken);
    Task<Affiliation> CreateAffiliationAsync(Affiliation affiliation, CancellationToken cancellationToken);
    Task<Affiliation?> GetAffiliationAsync(string affiliationId, CancellationToken cancellationToken);
    Task<Affiliation> UpdateAffiliationStatusAsync(Affiliation affiliation, string status, CancellationToken cancellationToken);
}
