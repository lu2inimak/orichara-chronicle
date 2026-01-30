using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Repositories;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests;

public class AuthOwnershipTests
{
    [Fact]
    public async Task UpdateCharacter_RequiresAuth()
    {
        var (auth, characterRepo) = CreateCharacterFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var updateCharacter = new UpdateCharacterUsecase(characterRepo, auth);

        var created = await createCharacter.ExecuteAsync(new CreateCharacterRequest(
            "user_1",
            "Alice",
            null,
            null
        ), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            updateCharacter.ExecuteAsync(new UpdateCharacterRequest(
                null,
                created.Id,
                new Dictionary<string, string> { ["Name"] = "Alice2" }
            ), CancellationToken.None));
    }

    [Fact]
    public async Task PostActivity_RejectsNonOwner()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo, activityRepo) = CreateWorldFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);

        var ownerId = "user_owner";
        var attackerId = "user_other";

        var ownerCharacter = await createCharacter.ExecuteAsync(new CreateCharacterRequest(ownerId, "Owner", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(ownerId, "World", null), CancellationToken.None);
        var affiliation = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(ownerId, world.Id, ownerCharacter.Id), CancellationToken.None);
        affiliation = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(ownerId, affiliation.Id), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            postActivity.ExecuteAsync(new PostActivityRequest(
                attackerId,
                affiliation.Id,
                "Should fail",
                null,
                null
            ), CancellationToken.None));
    }

    private static (MockAuthenticator auth, ICharacterRepository characters) CreateCharacterFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        return (auth, new DynamoCharacterRepository(db, options));
    }

    private static (MockAuthenticator auth, ICharacterRepository characters, IWorldRepository worlds, IAffiliationRepository affiliations, IActivityRepository activities) CreateWorldFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        var worldRepoImpl = new DynamoWorldRepository(db, options);
        return (auth, new DynamoCharacterRepository(db, options), worldRepoImpl, worldRepoImpl, new DynamoActivityRepository(db, options));
    }
}
