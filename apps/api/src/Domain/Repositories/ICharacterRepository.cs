using Api.Domain.Entities;

namespace Api.Domain.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetCharacterAsync(string characterId, CancellationToken cancellationToken);
    Task<Character> CreateCharacterAsync(string userId, Character character, CancellationToken cancellationToken);
    Task<Character> UpdateCharacterAsync(string characterId, Dictionary<string, string> updates, CancellationToken cancellationToken);
}
