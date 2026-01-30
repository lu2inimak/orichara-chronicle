using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Api.Modules.User.Models;
using Api.Shared.Infrastructure;

namespace Api.Modules.User.Store;

public sealed class DynamoUserStore : IUserStore
{
    private const string ProfileSK = "PROFILE";
    private const string OwnCharPrefix = "OWN_CHAR#";
    private const string OwnWorldPrefix = "OWN_WORLD#";

    private readonly IAmazonDynamoDB _dynamo;
    private readonly DynamoOptions _options;

    public DynamoUserStore(IAmazonDynamoDB dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task<MeResponse> GetMeAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("DDB table name is required");
        }

        var response = new MeResponse
        {
            User = new UserProfile { Id = userId }
        };

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
                    response.User.CreatedAt = createdAt.S;
                }
                if (item.TryGetValue("UpdatedAt", out var updatedAt))
                {
                    response.User.UpdatedAt = updatedAt.S;
                }
            }
            else if (sk.StartsWith(OwnCharPrefix, StringComparison.Ordinal))
            {
                response.OwnedCharacterIds.Add(sk.Substring(OwnCharPrefix.Length));
            }
            else if (sk.StartsWith(OwnWorldPrefix, StringComparison.Ordinal))
            {
                response.HostedWorldIds.Add(sk.Substring(OwnWorldPrefix.Length));
            }
        }

        return response;
    }
}
