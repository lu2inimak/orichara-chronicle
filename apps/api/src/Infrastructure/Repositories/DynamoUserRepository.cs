using Amazon.DynamoDBv2.Model;
using Api.Domain.Entities;
using Api.Domain.ReadModels;
using Api.Domain.Repositories;
using Api.Infrastructure;

namespace Api.Infrastructure.Repositories;

public sealed class DynamoUserRepository : IUserRepository
{
    private const string ProfileSK = "PROFILE";
    private const string OwnCharPrefix = "OWN_CHAR#";
    private const string OwnWorldPrefix = "OWN_WORLD#";

    private readonly IDynamoDbClient _dynamo;
    private readonly DynamoOptions _options;

    public DynamoUserRepository(IDynamoDbClient dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task<UserSnapshot> GetSnapshotAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("DDB table name is required");
        }

        var ownedCharacters = new List<string>();
        var hostedWorlds = new List<string>();
        var profile = new UserProfile { Id = userId };

        var request = new QueryRequest
        {
            TableName = _options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"USER#{userId}" }
            }
        };

        var result = await _dynamo.QueryAsync(request, cancellationToken);
        foreach (var item in result.Items)
        {
            if (!item.TryGetValue("SK", out var skAttr) || string.IsNullOrWhiteSpace(skAttr.S))
            {
                continue;
            }
            var sk = skAttr.S;
            if (sk == ProfileSK)
            {
                if (item.TryGetValue("CreatedAt", out var createdAt))
                {
                    profile.CreatedAt = createdAt.S;
                }
                if (item.TryGetValue("UpdatedAt", out var updatedAt))
                {
                    profile.UpdatedAt = updatedAt.S;
                }
            }
            else if (sk.StartsWith(OwnCharPrefix, StringComparison.Ordinal))
            {
                ownedCharacters.Add(sk.Substring(OwnCharPrefix.Length));
            }
            else if (sk.StartsWith(OwnWorldPrefix, StringComparison.Ordinal))
            {
                hostedWorlds.Add(sk.Substring(OwnWorldPrefix.Length));
            }
        }

        return new UserSnapshot
        {
            Profile = profile,
            OwnedCharacterIds = ownedCharacters.ToArray(),
            HostedWorldIds = hostedWorlds.ToArray()
        };
    }
}
