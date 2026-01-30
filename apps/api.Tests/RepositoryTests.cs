using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Repositories;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Repositories;
using System.Threading.Tasks;
using Xunit;

namespace Api.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task CreateCharacter_AddsUserPointer()
    {
        var tableName = "occ-main";
        var db = new InMemoryDynamoDbClient(tableName);
        var options = new DynamoOptions(tableName);
        var auth = new MockAuthenticator();

        ICharacterRepository characterRepo = new DynamoCharacterRepository(db, options);
        IUserRepository userRepo = new DynamoUserRepository(db, options);
        var characterUsecase = new CreateCharacterUsecase(characterRepo, auth);

        var created = await characterUsecase.ExecuteAsync(new CreateCharacterRequest(
            "user_1",
            "Alice",
            null,
            null
        ), CancellationToken.None);
        var snapshot = await userRepo.GetSnapshotAsync("user_1", CancellationToken.None);

        Assert.Contains(created.Id, snapshot.OwnedCharacterIds);
    }
}
