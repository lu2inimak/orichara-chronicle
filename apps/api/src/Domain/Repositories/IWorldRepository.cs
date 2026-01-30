using Api.Domain.Entities;

namespace Api.Domain.Repositories;

public interface IWorldRepository
{
    Task<World?> GetWorldAsync(string worldId, CancellationToken cancellationToken);
    Task<World> CreateWorldAsync(string hostId, World world, CancellationToken cancellationToken);
}
