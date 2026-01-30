using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class CreateCharacterUsecase : IUsecase<CreateCharacterRequest, Character>
{
    private readonly ICharacterRepository _repository;
    private readonly IAuthenticator _authenticator;

    public CreateCharacterUsecase(ICharacterRepository repository, IAuthenticator authenticator)
    {
        _repository = repository;
        _authenticator = authenticator;
    }

    public async Task<Character> ExecuteAsync(CreateCharacterRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("name is required");
        }

        var now = DateTime.UtcNow.ToString("O");
        var character = new Character
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _repository.CreateCharacterAsync(auth.UserId, character, cancellationToken);
    }
}
