using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Enums;
using Api.Domain.Repositories;
using Api.Domain.ReadModels;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests;

public class GovernanceHardeningTests
{
    [Fact]
    public async Task Join_IsIdempotent_ReturnsSameAffiliationId()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo) = CreateWorldFixture();
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);

        var userId = "user_join";
        var character = await createCharacter.ExecuteAsync(new CreateCharacterRequest(userId, "Joiner", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(userId, "World", null), CancellationToken.None);

        var first = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(userId, world.Id, character.Id), CancellationToken.None);
        var second = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(userId, world.Id, character.Id), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Sign_IsIdempotent_DoesNotErrorOnDuplicateSignature()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo) = CreateWorldFixture();
        var activityRepo = new DynamoActivityRepository(new InMemoryDynamoDbClient("occ-main"), new DynamoOptions("occ-main"));
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);
        var signActivity = new SignActivityUsecase(activityRepo, affiliationRepo, auth, new NullLogger<SignActivityUsecase>());

        var hostId = "user_host";
        var coUserId = "user_co";

        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(hostId, "Host", null, null), CancellationToken.None);
        var coChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(coUserId, "Co", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(hostId, "World", null), CancellationToken.None);

        var hostAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(hostId, world.Id, hostChar.Id), CancellationToken.None);
        var coAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(coUserId, world.Id, coChar.Id), CancellationToken.None);
        hostAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, hostAff.Id), CancellationToken.None);
        coAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, coAff.Id), CancellationToken.None);

        var pending = await postActivity.ExecuteAsync(new PostActivityRequest(hostId, hostAff.Id, "collab", new List<string> { coAff.Id }, null), CancellationToken.None);
        Assert.Equal(ActivityStatus.PendingMultiSig, pending.Status);

        var signed = await signActivity.ExecuteAsync(new SignActivityRequest(coUserId, pending.Id, coAff.Id), CancellationToken.None);
        Assert.Equal(ActivityStatus.Published, signed.Status);

        var duplicate = await signActivity.ExecuteAsync(new SignActivityRequest(coUserId, pending.Id, coAff.Id), CancellationToken.None);
        Assert.Equal(ActivityStatus.Published, duplicate.Status);
    }

    [Fact]
    public async Task Reject_MarksActivityAsRedacted()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo) = CreateWorldFixture();
        var activityRepo = new DynamoActivityRepository(new InMemoryDynamoDbClient("occ-main"), new DynamoOptions("occ-main"));
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

    [Fact]
    public async Task PendingActivities_TimeoutToArchived()
    {
        var (auth, characterRepo, worldRepo, affiliationRepo) = CreateWorldFixture();
        var activityRepo = new DynamoActivityRepository(new InMemoryDynamoDbClient("occ-main"), new DynamoOptions("occ-main"));
        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);
        var archive = new ArchivePendingActivityUsecase(activityRepo, auth);
        var getTimeline = new GetWorldTimelineUsecase(activityRepo, auth);

        var hostId = "user_host";
        var coUserId = "user_co";
        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(hostId, "Host", null, null), CancellationToken.None);
        var coChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(coUserId, "Co", null, null), CancellationToken.None);
        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(hostId, "World", null), CancellationToken.None);

        var hostAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(hostId, world.Id, hostChar.Id), CancellationToken.None);
        var coAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(coUserId, world.Id, coChar.Id), CancellationToken.None);
        hostAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, hostAff.Id), CancellationToken.None);
        coAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(hostId, coAff.Id), CancellationToken.None);

        var pending = await postActivity.ExecuteAsync(new PostActivityRequest(hostId, hostAff.Id, "collab", new List<string> { coAff.Id }, null), CancellationToken.None);
        Assert.Equal(ActivityStatus.PendingMultiSig, pending.Status);

        var record = await activityRepo.GetActivityAsync(pending.Id, CancellationToken.None);
        Assert.NotNull(record);

        // force expiry into past
        var expired = new ActivityRecord
        {
            Id = record!.Id,
            AffiliationId = record.AffiliationId,
            WorldId = record.WorldId,
            OwnerId = record.OwnerId,
            Content = record.Content,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5).ToString("O"),
            RequiredSignatures = record.RequiredSignatures,
            Signatures = record.Signatures,
            CoCreators = record.CoCreators
        };
        await activityRepo.UpdateActivityStatusAsync(expired, ActivityStatus.PendingMultiSig, hideFromTimeline: false, CancellationToken.None);

        var archived = await archive.ExecuteAsync(new ArchivePendingActivityRequest(hostId, pending.Id), CancellationToken.None);
        Assert.Equal(ActivityStatus.ArchivedPending, archived.Status);

        var timeline = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(hostId, world.Id, 50), CancellationToken.None);
        Assert.Empty(timeline);
    }

    private static (MockAuthenticator auth, ICharacterRepository characters, IWorldRepository worlds, IAffiliationRepository affiliations) CreateWorldFixture()
    {
        var db = new InMemoryDynamoDbClient("occ-main");
        var options = new DynamoOptions("occ-main");
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());
        var worldRepoImpl = new DynamoWorldRepository(db, options);
        return (auth, new DynamoCharacterRepository(db, options), worldRepoImpl, worldRepoImpl);
    }
}
