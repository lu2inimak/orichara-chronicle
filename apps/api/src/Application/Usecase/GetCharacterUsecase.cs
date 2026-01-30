using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class GetCharacterUsecase : IUsecase<GetCharacterRequest, Character?>
{
    private readonly ICharacterRepository _repository;

    public GetCharacterUsecase(ICharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<Character?> ExecuteAsync(GetCharacterRequest request, CancellationToken cancellationToken)
    {
        return _repository.GetCharacterAsync(request.CharacterId, cancellationToken);
    }
}
