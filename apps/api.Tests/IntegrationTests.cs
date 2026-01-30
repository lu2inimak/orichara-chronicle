using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Enums;
using Api.Domain.Repositories;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task CharacterOwnership_IsEnforced()
    {
        var (auth, characterRepo) = CreateCharacterFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var updateCharacter = new UpdateCharacterUsecase(characterRepo, auth);

        var created = await createCharacter.ExecuteAsync(new CreateCharacterRequest(
            "user_owner",
            "Alice",
            null,
            null
        ), CancellationToken.None);

        var updated = await updateCharacter.ExecuteAsync(new UpdateCharacterRequest(
            "user_owner",
            created.Id,
            new Dictionary<string, string> { ["Name"] = "Alice2" }
        ), CancellationToken.None);
        Assert.Equal("Alice2", updated.Name);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            updateCharacter.ExecuteAsync(new UpdateCharacterRequest(
                "user_other",
                created.Id,
                new Dictionary<string, string> { ["Name"] = "Hacker" }
            ), CancellationToken.None));
    }

    [Fact]
    public async Task WorldJoin_ThenApproval_ActivatesAffiliation()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo) = CreateWorldFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);

        var hostId = "user_host";
        var joinerId = "user_joiner";
        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(hostId, "Host", null, null), CancellationToken.None);
        var joinerChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(joinerId, "Joiner", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(hostId, "World", null), CancellationToken.None);

        var pending = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(joinerId, world.Id, joinerChar.Id), CancellationToken.None);
        Assert.Equal(AffiliationStatus.Pending, pending.Status);

        var approved = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, pending.Id), CancellationToken.None);
        Assert.Equal(AffiliationStatus.Active, approved.Status);
        Assert.Equal(hostChar.OwnerId, hostId);
    }

    [Fact]
    public async Task MultiSigPromotion_PublishesAfterAllSignatures()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo, activityRepo) = CreateWorldActivityFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);
        var signActivity = new SignActivityUsecase(activityRepo, affiliationRepo, auth, new NullLogger<SignActivityUsecase>());
        var getTimeline = new GetWorldTimelineUsecase(activityRepo, auth);

        var hostId = "user_host";
        var coId = "user_co";
        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(hostId, "Host", null, null), CancellationToken.None);
        var coChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(coId, "Co", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(hostId, "World", null), CancellationToken.None);

        var hostAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(hostId, world.Id, hostChar.Id), CancellationToken.None);
        var coAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(coId, world.Id, coChar.Id), CancellationToken.None);
        hostAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, hostAff.Id), CancellationToken.None);
        coAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, coAff.Id), CancellationToken.None);

        var pending = await postActivity.ExecuteAsync(new PostActivityRequest(
            hostId,
            hostAff.Id,
            "collab",
            new List<string> { coAff.Id },
            null
        ), CancellationToken.None);
        Assert.Equal(ActivityStatus.PendingMultiSig, pending.Status);

        var timelineBefore = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(hostId, world.Id, 50), CancellationToken.None);
        Assert.Empty(timelineBefore);

        var signed = await signActivity.ExecuteAsync(new SignActivityRequest(coId, pending.Id, coAff.Id), CancellationToken.None);
        Assert.Equal(ActivityStatus.Published, signed.Status);

        var timelineAfter = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(hostId, world.Id, 50), CancellationToken.None);
        Assert.Single(timelineAfter);
    }

    [Fact]
    public async Task Timeline_FiltersOutRedactedActivities()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo, activityRepo) = CreateWorldActivityFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);
        var rejectActivity = new RejectActivityUsecase(activityRepo, affiliationRepo, auth);
        var getTimeline = new GetWorldTimelineUsecase(activityRepo, auth);

        var hostId = "user_host";
        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(hostId, "Host", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(hostId, "World", null), CancellationToken.None);
        var hostAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(hostId, world.Id, hostChar.Id), CancellationToken.None);
        hostAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, hostAff.Id), CancellationToken.None);

        var created = await postActivity.ExecuteAsync(new PostActivityRequest(hostId, hostAff.Id, "hello", null, null), CancellationToken.None);
        Assert.Equal(ActivityStatus.Published, created.Status);

        var rejected = await rejectActivity.ExecuteAsync(new RejectActivityRequest(hostId, created.Id, hostAff.Id), CancellationToken.None);
        Assert.Equal(ActivityStatus.Redacted, rejected.Status);

        var timeline = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(hostId, world.Id, 50), CancellationToken.None);
        Assert.Empty(timeline);
    }

    private static (MockAuthenticator auth, ICharacterRepository characters) CreateCharacterFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        return (auth, new DynamoCharacterRepository(db, options));
    }

    private static (MockAuthenticator auth, ICharacterRepository characters, IWorldRepository worlds, IAffiliationRepository affiliations) CreateWorldFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        var worldRepoImpl = new DynamoWorldRepository(db, options);
        return (auth, new DynamoCharacterRepository(db, options), worldRepoImpl, worldRepoImpl);
    }

    private static (MockAuthenticator auth, ICharacterRepository characters, IWorldRepository worlds, IAffiliationRepository affiliations, IActivityRepository activities) CreateWorldActivityFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        var worldRepoImpl = new DynamoWorldRepository(db, options);
        return (auth, new DynamoCharacterRepository(db, options), worldRepoImpl, worldRepoImpl, new DynamoActivityRepository(db, options));
    }
}
