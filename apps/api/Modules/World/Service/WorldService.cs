using Api.Modules.Characters.Store;
using Api.Modules.World.Models;
using Api.Modules.World.Store;

namespace Api.Modules.World.Service;

public sealed class WorldService
{
    private readonly IWorldStore _store;
    private readonly ICharacterStore _characters;

    public WorldService(IWorldStore store, ICharacterStore characters)
    {
        _store = store;
        _characters = characters;
    }

    public async Task<Models.World> CreateWorldAsync(string hostId, string name, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name is required");
        }

        var now = DateTime.UtcNow.ToString("O");
        var world = new Models.World
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _store.CreateWorldAsync(hostId, world, cancellationToken);
    }

    public async Task<Affiliation> RequestJoinAsync(string userId, string worldId, string characterId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(worldId))
        {
            throw new ArgumentException("world_id is required");
        }
        if (string.IsNullOrWhiteSpace(characterId))
        {
            throw new ArgumentException("character_id is required");
        }

        var world = await _store.GetWorldAsync(worldId, cancellationToken);
        if (world == null)
        {
            throw new KeyNotFoundException("world_not_found");
        }

        var character = await _characters.GetCharacterAsync(characterId, cancellationToken);
        if (character == null)
        {
            throw new KeyNotFoundException("character_not_found");
        }
        if (!string.Equals(character.OwnerId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        var now = DateTime.UtcNow.ToString("O");
        var affiliation = new Affiliation
        {
            Id = Guid.NewGuid().ToString("N"),
            WorldId = worldId,
            CharacterId = characterId,
            OwnerId = userId,
            Status = AffiliationStatuses.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _store.CreateAffiliationAsync(affiliation, cancellationToken);
    }

    public async Task<Affiliation> ApproveAffiliationAsync(string userId, string affiliationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(affiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }

        var affiliation = await _store.GetAffiliationAsync(affiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }

        var world = await _store.GetWorldAsync(affiliation.WorldId, cancellationToken);
        if (world == null)
        {
            throw new KeyNotFoundException("world_not_found");
        }
        if (!string.Equals(world.HostId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("forbidden");
        }

        return await _store.UpdateAffiliationStatusAsync(affiliation, AffiliationStatuses.Active, cancellationToken);
    }
}
