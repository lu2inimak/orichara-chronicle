using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class CreateWorldUsecase : IUsecase<CreateWorldRequest, World>
{
    private readonly IWorldRepository _repository;
    private readonly IAuthenticator _authenticator;

    public CreateWorldUsecase(IWorldRepository repository, IAuthenticator authenticator)
    {
        _repository = repository;
        _authenticator = authenticator;
    }

    public async Task<World> ExecuteAsync(CreateWorldRequest request, CancellationToken cancellationToken)
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
        var world = new World
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _repository.CreateWorldAsync(auth.UserId, world, cancellationToken);
    }
}
