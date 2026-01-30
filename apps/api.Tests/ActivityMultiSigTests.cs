using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Enums;
using Api.Domain.Repositories;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Xunit;

namespace Api.Tests;

public class ActivityMultiSigTests
{
    [Fact]
    public async Task MultiSigActivity_PublishesAfterAllSignatures()
    {
        var tableName = "occ-main";
        var db = new InMemoryDynamoDbClient(tableName);
        var options = new DynamoOptions(tableName);
        var auth = new MockAuthenticator(new NullLogger<MockAuthenticator>());

        ICharacterRepository characterRepo = new DynamoCharacterRepository(db, options);
        var worldRepoImpl = new DynamoWorldRepository(db, options);
        IWorldRepository worldRepo = worldRepoImpl;
        IAffiliationRepository affiliationRepo = worldRepoImpl;
        IActivityRepository activityRepo = new DynamoActivityRepository(db, options);

        var createCharacter = new CreateCharacterUsecase(characterRepo, auth);
        var createWorld = new CreateWorldUsecase(worldRepo, auth);
        var requestJoin = new RequestJoinWorldUsecase(worldRepo, affiliationRepo, characterRepo, auth);
        var approveAffiliation = new ApproveAffiliationUsecase(worldRepo, affiliationRepo, auth);
        var postActivity = new PostActivityUsecase(activityRepo, affiliationRepo, auth);
        var signActivity = new SignActivityUsecase(activityRepo, affiliationRepo, auth, new NullLogger<SignActivityUsecase>());
        var getTimeline = new GetWorldTimelineUsecase(activityRepo, auth);

        var hostId = "user_host";
        var coUserId = "user_b";

        var hostChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(
            hostId,
            "HostChar",
            null,
            null
        ), CancellationToken.None);
        var coChar = await createCharacter.ExecuteAsync(new CreateCharacterRequest(
            coUserId,
            "CoChar",
            null,
            null
        ), CancellationToken.None);

        var world = await createWorld.ExecuteAsync(new CreateWorldRequest(
            hostId,
            "World",
            null
        ), CancellationToken.None);
        var hostAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(
            hostId,
            world.Id,
            hostChar.Id
        ), CancellationToken.None);
        var coAff = await requestJoin.ExecuteAsync(new RequestJoinWorldRequest(
            coUserId,
            world.Id,
            coChar.Id
        ), CancellationToken.None);

        hostAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(
            hostId,
            hostAff.Id
        ), CancellationToken.None);
        coAff = await approveAffiliation.ExecuteAsync(new ApproveAffiliationRequest(
            hostId,
            coAff.Id
        ), CancellationToken.None);

        var pending = await postActivity.ExecuteAsync(new PostActivityRequest(
            hostId,
            hostAff.Id,
            "collab",
            new List<string> { coAff.Id },
            null
        ), CancellationToken.None);
        Assert.Equal(ActivityStatus.PendingMultiSig, pending.Status);

        var timelineBefore = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(
            hostId,
            world.Id,
            50
        ), CancellationToken.None);
        Assert.Empty(timelineBefore);

        var signed = await signActivity.ExecuteAsync(new SignActivityRequest(
            coUserId,
            pending.Id,
            coAff.Id
        ), CancellationToken.None);
        Assert.Equal(ActivityStatus.Published, signed.Status);

        var timelineAfter = await getTimeline.ExecuteAsync(new GetWorldTimelineRequest(
            hostId,
            world.Id,
            50
        ), CancellationToken.None);
        Assert.Single(timelineAfter);
    }
}
