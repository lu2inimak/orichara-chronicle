using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class RequestJoinWorldUsecase : IUsecase<RequestJoinWorldRequest, Affiliation>
{
    private readonly IWorldRepository _repository;
    private readonly IAffiliationRepository _affiliations;
    private readonly ICharacterRepository _characters;
    private readonly IAuthenticator _authenticator;

    public RequestJoinWorldUsecase(IWorldRepository repository, IAffiliationRepository affiliations, ICharacterRepository characters, IAuthenticator authenticator)
    {
        _repository = repository;
        _affiliations = affiliations;
        _characters = characters;
        _authenticator = authenticator;
    }

    public async Task<Affiliation> ExecuteAsync(RequestJoinWorldRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.WorldId))
        {
            throw new ArgumentException("world_id is required");
        }
        if (string.IsNullOrWhiteSpace(request.CharacterId))
        {
            throw new ArgumentException("character_id is required");
        }

        var world = await _repository.GetWorldAsync(request.WorldId, cancellationToken);
        if (world == null)
        {
            throw new KeyNotFoundException("world_not_found");
        }

        var character = await _characters.GetCharacterAsync(request.CharacterId, cancellationToken);
        if (character == null)
        {
            throw new KeyNotFoundException("character_not_found");
        }
        if (!string.Equals(character.OwnerId, auth.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        var now = DateTime.UtcNow.ToString("O");
        var affiliation = new Affiliation
        {
            Id = Guid.NewGuid().ToString("N"),
            WorldId = request.WorldId,
            CharacterId = request.CharacterId,
            OwnerId = auth.UserId,
            Status = AffiliationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _affiliations.CreateAffiliationAsync(affiliation, cancellationToken);
    }
}
