using Api.Modules.Characters.Models;
using Api.Modules.Characters.Store;

namespace Api.Modules.Characters.Service;

public sealed class CharacterService
{
    private readonly ICharacterStore _store;

    public CharacterService(ICharacterStore store)
    {
        _store = store;
    }

    public Task<Character?> GetCharacterAsync(string characterId, CancellationToken cancellationToken)
    {
        return _store.GetCharacterAsync(characterId, cancellationToken);
    }

    public async Task<Character> CreateCharacterAsync(string userId, string name, string? bio, string? avatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name is required");
        }

        var now = DateTime.UtcNow.ToString("O");
        var character = new Character
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _store.CreateCharacterAsync(userId, character, cancellationToken);
    }

    public async Task<Character> UpdateCharacterAsync(string userId, string characterId, Dictionary<string, string> updates, CancellationToken cancellationToken)
    {
        var current = await _store.GetCharacterAsync(characterId, cancellationToken);
        if (current == null)
        {
            throw new KeyNotFoundException("not_found");
        }
        if (!string.Equals(current.OwnerId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        if (updates.Count == 0)
        {
            throw new ArgumentException("no_updates");
        }

        updates["UpdatedAt"] = DateTime.UtcNow.ToString("O");
        return await _store.UpdateCharacterAsync(characterId, updates, cancellationToken);
    }
}
