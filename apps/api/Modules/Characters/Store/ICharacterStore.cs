using Api.Modules.Characters.Models;

namespace Api.Modules.Characters.Store;

public interface ICharacterStore
{
    Task<Character?> GetCharacterAsync(string characterId, CancellationToken cancellationToken);
    Task<Character> CreateCharacterAsync(string userId, Character character, CancellationToken cancellationToken);
    Task<Character> UpdateCharacterAsync(string characterId, Dictionary<string, string> updates, CancellationToken cancellationToken);
}
