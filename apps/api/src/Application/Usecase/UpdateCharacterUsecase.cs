using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class UpdateCharacterUsecase : IUsecase<UpdateCharacterRequest, Character>
{
    private readonly ICharacterRepository _repository;
    private readonly IAuthenticator _authenticator;

    public UpdateCharacterUsecase(ICharacterRepository repository, IAuthenticator authenticator)
    {
        _repository = repository;
        _authenticator = authenticator;
    }

    public async Task<Character> ExecuteAsync(UpdateCharacterRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        var current = await _repository.GetCharacterAsync(request.CharacterId, cancellationToken);
        if (current == null)
        {
            throw new KeyNotFoundException("not_found");
        }
        if (!string.Equals(current.OwnerId, auth.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        if (request.Updates.Count == 0)
        {
            throw new ArgumentException("no_updates");
        }

        request.Updates["UpdatedAt"] = DateTime.UtcNow.ToString("O");
        return await _repository.UpdateCharacterAsync(request.CharacterId, request.Updates, cancellationToken);
    }
}
