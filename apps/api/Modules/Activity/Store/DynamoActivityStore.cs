using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Api.Modules.Activity.Models;
using Api.Shared.Infrastructure;

namespace Api.Modules.Activity.Store;

public sealed class DynamoActivityStore : IActivityStore
{
    private const string GsiTimeline = "GSI_Timeline";

    private readonly IAmazonDynamoDB _dynamo;
    private readonly DynamoOptions _options;

    public DynamoActivityStore(IAmazonDynamoDB dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task<AffiliationRecord?> GetAffiliationAsync(string affiliationId, CancellationToken cancellationToken)
    {
        EnsureTable();
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"AFF#{affiliationId}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            }
        }, cancellationToken);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        return new AffiliationRecord
        {
            Id = affiliationId,
            WorldId = response.Item.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            OwnerId = response.Item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Status = response.Item.TryGetValue("Status", out var status) ? status.S ?? string.Empty : string.Empty
        };
    }

    public async Task<Models.Activity> CreateActivityAsync(Models.Activity activity, List<string> requiredSignatures, List<string> signatures, bool publishTimeline, CancellationToken cancellationToken)
    {
        EnsureTable();

        var activityMeta = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"ACT#{activity.Id}" },
            ["SK"] = new AttributeValue { S = "INFO" },
            ["ActivityID"] = new AttributeValue { S = activity.Id },
            ["AffiliationID"] = new AttributeValue { S = activity.AffiliationId },
            ["WorldID"] = new AttributeValue { S = activity.WorldId },
            ["OwnerID"] = new AttributeValue { S = activity.OwnerId },
            ["Content"] = new AttributeValue { S = activity.Content },
            ["Status"] = new AttributeValue { S = activity.Status },
            ["CreatedAt"] = new AttributeValue { S = activity.CreatedAt },
            ["CoCreators"] = ToStringList(activity.CoCreatorIds),
            ["RequiredSignatures"] = ToStringList(requiredSignatures),
            ["Signatures"] = ToStringList(signatures)
        };

        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"AFF#{activity.AffiliationId}" },
            ["SK"] = new AttributeValue { S = $"ACT#{activity.CreatedAt}" },
            ["ActivityID"] = new AttributeValue { S = activity.Id },
            ["AffiliationID"] = new AttributeValue { S = activity.AffiliationId },
            ["WorldID"] = new AttributeValue { S = activity.WorldId },
            ["OwnerID"] = new AttributeValue { S = activity.OwnerId },
            ["Content"] = new AttributeValue { S = activity.Content },
            ["Status"] = new AttributeValue { S = activity.Status },
            ["CreatedAt"] = new AttributeValue { S = activity.CreatedAt }
        };
        if (publishTimeline)
        {
            item["GSI_TimelinePK"] = new AttributeValue { S = $"WORLD#{activity.WorldId}" };
            item["GSI_TimelineSK"] = new AttributeValue { S = $"ACT#{activity.CreatedAt}" };
        }

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new() { Put = new Put { TableName = _options.TableName, Item = activityMeta } },
                new() { Put = new Put { TableName = _options.TableName, Item = item } }
            }
        }, cancellationToken);

        return activity;
    }

    public async Task<ActivityRecord?> GetActivityAsync(string activityId, CancellationToken cancellationToken)
    {
        EnsureTable();
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"ACT#{activityId}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            }
        }, cancellationToken);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        return new ActivityRecord
        {
            Id = activityId,
            AffiliationId = response.Item.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
            WorldId = response.Item.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            OwnerId = response.Item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Content = response.Item.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
            Status = response.Item.TryGetValue("Status", out var status) ? status.S ?? string.Empty : string.Empty,
            CreatedAt = response.Item.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty,
            CoCreators = FromStringList(response.Item.TryGetValue("CoCreators", out var cc) ? cc : null),
            RequiredSignatures = FromStringList(response.Item.TryGetValue("RequiredSignatures", out var req) ? req : null),
            Signatures = FromStringList(response.Item.TryGetValue("Signatures", out var sig) ? sig : null)
        };
    }

    public async Task<ActivityRecord> UpdateActivitySignaturesAsync(ActivityRecord record, List<string> signatures, string status, CancellationToken cancellationToken)
    {
        EnsureTable();
        var response = await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"ACT#{record.Id}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            },
            UpdateExpression = "SET #Signatures = :Signatures, #Status = :Status",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#Signatures"] = "Signatures",
                ["#Status"] = "Status"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":Signatures"] = ToStringList(signatures),
                [":Status"] = new AttributeValue { S = status }
            },
            ReturnValues = ReturnValue.ALL_NEW
        }, cancellationToken);

        return new ActivityRecord
        {
            Id = record.Id,
            AffiliationId = response.Attributes.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
            WorldId = response.Attributes.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            OwnerId = response.Attributes.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Content = response.Attributes.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
            Status = response.Attributes.TryGetValue("Status", out var statusValue) ? statusValue.S ?? string.Empty : string.Empty,
            CreatedAt = response.Attributes.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty,
            CoCreators = FromStringList(response.Attributes.TryGetValue("CoCreators", out var cc) ? cc : null),
            RequiredSignatures = FromStringList(response.Attributes.TryGetValue("RequiredSignatures", out var req) ? req : null),
            Signatures = FromStringList(response.Attributes.TryGetValue("Signatures", out var sig) ? sig : null)
        };
    }

    public async Task PublishActivityAsync(ActivityRecord record, CancellationToken cancellationToken)
    {
        EnsureTable();
        await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"AFF#{record.AffiliationId}" },
                ["SK"] = new AttributeValue { S = $"ACT#{record.CreatedAt}" }
            },
            UpdateExpression = "SET #Status = :Status, #GsiPk = :GsiPk, #GsiSk = :GsiSk",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#Status"] = "Status",
                ["#GsiPk"] = "GSI_TimelinePK",
                ["#GsiSk"] = "GSI_TimelineSK"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":Status"] = new AttributeValue { S = ActivityStatuses.Published },
                [":GsiPk"] = new AttributeValue { S = $"WORLD#{record.WorldId}" },
                [":GsiSk"] = new AttributeValue { S = $"ACT#{record.CreatedAt}" }
            }
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<Models.Activity>> ListWorldTimelineAsync(string worldId, int limit, CancellationToken cancellationToken)
    {
        EnsureTable();
        if (limit <= 0)
        {
            limit = 50;
        }

        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName = _options.TableName,
            IndexName = GsiTimeline,
            KeyConditionExpression = "GSI_TimelinePK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"WORLD#{worldId}" }
            },
            ScanIndexForward = false,
            Limit = limit
        }, cancellationToken);

        var items = new List<Models.Activity>(response.Items.Count);
        foreach (var item in response.Items)
        {
            items.Add(new Models.Activity
            {
                Id = item.TryGetValue("ActivityID", out var id) ? id.S ?? string.Empty : string.Empty,
                AffiliationId = item.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
                WorldId = item.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
                OwnerId = item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
                Content = item.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
                Status = item.TryGetValue("Status", out var status) ? status.S ?? string.Empty : string.Empty,
                CreatedAt = item.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty
            });
        }

        return items;
    }

    private void EnsureTable()
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("DDB table name is required");
        }
    }

    private static AttributeValue ToStringList(IEnumerable<string> values)
    {
        var list = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => new AttributeValue { S = v })
            .ToList();
        return new AttributeValue { L = list };
    }

    private static List<string> FromStringList(AttributeValue? value)
    {
        if (value == null || value.L == null)
        {
            return new List<string>();
        }
        return value.L.Select(v => v.S ?? string.Empty).Where(v => v.Length > 0).ToList();
    }
}
