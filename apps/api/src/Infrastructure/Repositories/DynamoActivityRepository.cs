using Amazon.DynamoDBv2.Model;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.ReadModels;
using Api.Domain.Repositories;

namespace Api.Infrastructure.Repositories;

public sealed class DynamoActivityRepository : IActivityRepository
{
    private const string GsiTimeline = "GSI_Timeline";

    private readonly IDynamoDbClient _dynamo;
    private readonly DynamoOptions _options;

    public DynamoActivityRepository(IDynamoDbClient dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }


    public async Task<Activity> CreateActivityAsync(Activity activity, List<string> requiredSignatures, List<string> signatures, bool publishTimeline, CancellationToken cancellationToken)
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
            ["Status"] = new AttributeValue { S = activity.Status.ToString() },
            ["CreatedAt"] = new AttributeValue { S = activity.CreatedAt },
            ["ExpiresAt"] = new AttributeValue { S = activity.ExpiresAt ?? string.Empty },
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
            ["Status"] = new AttributeValue { S = activity.Status.ToString() },
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
            Status = ParseStatus(response.Item.TryGetValue("Status", out var status) ? status.S : null),
            CreatedAt = response.Item.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty,
            ExpiresAt = response.Item.TryGetValue("ExpiresAt", out var expires) ? expires.S : null,
            CoCreators = FromStringList(response.Item.TryGetValue("CoCreators", out var cc) ? cc : null),
            RequiredSignatures = FromStringList(response.Item.TryGetValue("RequiredSignatures", out var req) ? req : null),
            Signatures = FromStringList(response.Item.TryGetValue("Signatures", out var sig) ? sig : null)
        };
    }

    public async Task<ActivityRecord> UpdateActivitySignaturesAsync(ActivityRecord record, List<string> signatures, ActivityStatus status, CancellationToken cancellationToken)
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
                [":Status"] = new AttributeValue { S = status.ToString() }
            },
            ReturnValues = "ALL_NEW"
        }, cancellationToken);

        return new ActivityRecord
        {
            Id = record.Id,
            AffiliationId = response.Attributes.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
            WorldId = response.Attributes.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            OwnerId = response.Attributes.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Content = response.Attributes.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
            Status = ParseStatus(response.Attributes.TryGetValue("Status", out var statusValue) ? statusValue.S : null),
            CreatedAt = response.Attributes.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty,
            ExpiresAt = response.Attributes.TryGetValue("ExpiresAt", out var expires) ? expires.S : null,
            CoCreators = FromStringList(response.Attributes.TryGetValue("CoCreators", out var cc) ? cc : null),
            RequiredSignatures = FromStringList(response.Attributes.TryGetValue("RequiredSignatures", out var req) ? req : null),
            Signatures = FromStringList(response.Attributes.TryGetValue("Signatures", out var sig) ? sig : null)
        };
    }

    public async Task<ActivityRecord> UpdateActivityStatusAsync(ActivityRecord record, ActivityStatus status, bool hideFromTimeline, CancellationToken cancellationToken)
    {
        EnsureTable();
        var setParts = new List<string> { "#Status = :Status" };
        var names = new Dictionary<string, string> { ["#Status"] = "Status" };
        var values = new Dictionary<string, AttributeValue>
        {
            [":Status"] = new AttributeValue { S = status.ToString() }
        };
        if (!string.IsNullOrWhiteSpace(record.ExpiresAt))
        {
            setParts.Add("#ExpiresAt = :ExpiresAt");
            names["#ExpiresAt"] = "ExpiresAt";
            values[":ExpiresAt"] = new AttributeValue { S = record.ExpiresAt };
        }

        var response = await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"ACT#{record.Id}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            },
            UpdateExpression = "SET " + string.Join(", ", setParts),
            ExpressionAttributeNames = names,
            ExpressionAttributeValues = values,
            ReturnValues = "ALL_NEW"
        }, cancellationToken);

        var timelineUpdateExpr = hideFromTimeline
            ? "SET #Status = :Status, #GsiPk = :Empty, #GsiSk = :Empty"
            : "SET #Status = :Status";

        var timelineNames = new Dictionary<string, string> { ["#Status"] = "Status" };
        var timelineValues = new Dictionary<string, AttributeValue>
        {
            [":Status"] = new AttributeValue { S = status.ToString() }
        };

        if (hideFromTimeline)
        {
            timelineNames["#GsiPk"] = "GSI_TimelinePK";
            timelineNames["#GsiSk"] = "GSI_TimelineSK";
            timelineValues[":Empty"] = new AttributeValue { S = string.Empty };
        }

        await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"AFF#{record.AffiliationId}" },
                ["SK"] = new AttributeValue { S = $"ACT#{record.CreatedAt}" }
            },
            UpdateExpression = timelineUpdateExpr,
            ExpressionAttributeNames = timelineNames,
            ExpressionAttributeValues = timelineValues
        }, cancellationToken);

        return new ActivityRecord
        {
            Id = record.Id,
            AffiliationId = response.Attributes.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
            WorldId = response.Attributes.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            OwnerId = response.Attributes.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Content = response.Attributes.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
            Status = ParseStatus(response.Attributes.TryGetValue("Status", out var statusValue) ? statusValue.S : null),
            CreatedAt = response.Attributes.TryGetValue("CreatedAt", out var created) ? created.S ?? string.Empty : string.Empty,
            ExpiresAt = response.Attributes.TryGetValue("ExpiresAt", out var expires) ? expires.S : null,
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
                [":Status"] = new AttributeValue { S = ActivityStatus.Published.ToString() },
                [":GsiPk"] = new AttributeValue { S = $"WORLD#{record.WorldId}" },
                [":GsiSk"] = new AttributeValue { S = $"ACT#{record.CreatedAt}" }
            }
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> ListWorldTimelineAsync(string worldId, int limit, CancellationToken cancellationToken)
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

        var items = new List<Activity>(response.Items.Count);
        foreach (var item in response.Items)
        {
            items.Add(new Activity
            {
                Id = item.TryGetValue("ActivityID", out var id) ? id.S ?? string.Empty : string.Empty,
                AffiliationId = item.TryGetValue("AffiliationID", out var aff) ? aff.S ?? string.Empty : string.Empty,
                WorldId = item.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
                OwnerId = item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
                Content = item.TryGetValue("Content", out var content) ? content.S ?? string.Empty : string.Empty,
                Status = ParseStatus(item.TryGetValue("Status", out var status) ? status.S : null),
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

    private static ActivityStatus ParseStatus(string? value)
    {
        return Enum.TryParse<ActivityStatus>(value, ignoreCase: false, out var parsed)
            ? parsed
            : ActivityStatus.Published;
    }

}
