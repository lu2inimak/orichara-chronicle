using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Api.Modules.World.Models;
using Api.Shared.Infrastructure;

namespace Api.Modules.World.Store;

public sealed class DynamoWorldStore : IWorldStore
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly DynamoOptions _options;

    public DynamoWorldStore(IAmazonDynamoDB dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task<Models.World?> GetWorldAsync(string worldId, CancellationToken cancellationToken)
    {
        EnsureTable();
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"WORLD#{worldId}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            }
        }, cancellationToken);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        return MapWorld(response.Item, worldId);
    }

    public async Task<Models.World> CreateWorldAsync(string hostId, Models.World world, CancellationToken cancellationToken)
    {
        EnsureTable();
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"WORLD#{world.Id}" },
            ["SK"] = new AttributeValue { S = "INFO" },
            ["HostID"] = new AttributeValue { S = hostId },
            ["Name"] = new AttributeValue { S = world.Name },
            ["CreatedAt"] = new AttributeValue { S = world.CreatedAt },
            ["UpdatedAt"] = new AttributeValue { S = world.UpdatedAt }
        };
        if (!string.IsNullOrWhiteSpace(world.Description))
        {
            item["Description"] = new AttributeValue { S = world.Description };
        }

        var pointer = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{hostId}" },
            ["SK"] = new AttributeValue { S = $"OWN_WORLD#{world.Id}" },
            ["RefType"] = new AttributeValue { S = "World" },
            ["RefID"] = new AttributeValue { S = world.Id }
        };

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new() { Put = new Put { TableName = _options.TableName, Item = item } },
                new() { Put = new Put { TableName = _options.TableName, Item = pointer } }
            }
        }, cancellationToken);

        world.HostId = hostId;
        return world;
    }

    public async Task<Affiliation> CreateAffiliationAsync(Affiliation affiliation, CancellationToken cancellationToken)
    {
        EnsureTable();
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"AFF#{affiliation.Id}" },
            ["SK"] = new AttributeValue { S = "INFO" },
            ["WorldID"] = new AttributeValue { S = affiliation.WorldId },
            ["CharacterID"] = new AttributeValue { S = affiliation.CharacterId },
            ["OwnerID"] = new AttributeValue { S = affiliation.OwnerId },
            ["Status"] = new AttributeValue { S = affiliation.Status },
            ["CreatedAt"] = new AttributeValue { S = affiliation.CreatedAt },
            ["UpdatedAt"] = new AttributeValue { S = affiliation.UpdatedAt }
        };

        var worldAff = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"WORLD#{affiliation.WorldId}" },
            ["SK"] = new AttributeValue { S = $"AFF#{affiliation.CharacterId}" },
            ["AffiliationID"] = new AttributeValue { S = affiliation.Id },
            ["CharacterID"] = new AttributeValue { S = affiliation.CharacterId },
            ["OwnerID"] = new AttributeValue { S = affiliation.OwnerId },
            ["Status"] = new AttributeValue { S = affiliation.Status }
        };

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new() { Put = new Put { TableName = _options.TableName, Item = item } },
                new() { Put = new Put { TableName = _options.TableName, Item = worldAff } }
            }
        }, cancellationToken);

        return affiliation;
    }

    public async Task<Affiliation?> GetAffiliationAsync(string affiliationId, CancellationToken cancellationToken)
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

        return MapAffiliation(response.Item, affiliationId);
    }

    public async Task<Affiliation> UpdateAffiliationStatusAsync(Affiliation affiliation, string status, CancellationToken cancellationToken)
    {
        EnsureTable();
        var updatedAt = DateTime.UtcNow.ToString("O");

        var affKey = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"AFF#{affiliation.Id}" },
            ["SK"] = new AttributeValue { S = "INFO" }
        };
        var worldKey = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"WORLD#{affiliation.WorldId}" },
            ["SK"] = new AttributeValue { S = $"AFF#{affiliation.CharacterId}" }
        };

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new()
                {
                    Update = new Update
                    {
                        TableName = _options.TableName,
                        Key = affKey,
                        UpdateExpression = "SET #Status = :Status, #UpdatedAt = :UpdatedAt",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#Status"] = "Status",
                            ["#UpdatedAt"] = "UpdatedAt"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":Status"] = new AttributeValue { S = status },
                            [":UpdatedAt"] = new AttributeValue { S = updatedAt }
                        }
                    }
                },
                new()
                {
                    Update = new Update
                    {
                        TableName = _options.TableName,
                        Key = worldKey,
                        UpdateExpression = "SET #Status = :Status",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#Status"] = "Status"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":Status"] = new AttributeValue { S = status }
                        }
                    }
                }
            }
        }, cancellationToken);

        affiliation.Status = status;
        affiliation.UpdatedAt = updatedAt;
        return affiliation;
    }

    private void EnsureTable()
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("DDB table name is required");
        }
    }

    private static Models.World MapWorld(Dictionary<string, AttributeValue> item, string worldId)
    {
        return new Models.World
        {
            Id = worldId,
            HostId = item.TryGetValue("HostID", out var host) ? host.S ?? string.Empty : string.Empty,
            Name = item.TryGetValue("Name", out var name) ? name.S ?? string.Empty : string.Empty,
            Description = item.TryGetValue("Description", out var desc) ? desc.S : null,
            CreatedAt = item.TryGetValue("CreatedAt", out var createdAt) ? createdAt.S ?? string.Empty : string.Empty,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var updatedAt) ? updatedAt.S ?? string.Empty : string.Empty
        };
    }

    private static Affiliation MapAffiliation(Dictionary<string, AttributeValue> item, string affiliationId)
    {
        return new Affiliation
        {
            Id = affiliationId,
            WorldId = item.TryGetValue("WorldID", out var world) ? world.S ?? string.Empty : string.Empty,
            CharacterId = item.TryGetValue("CharacterID", out var character) ? character.S ?? string.Empty : string.Empty,
            OwnerId = item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Status = item.TryGetValue("Status", out var status) ? status.S ?? string.Empty : string.Empty,
            CreatedAt = item.TryGetValue("CreatedAt", out var createdAt) ? createdAt.S ?? string.Empty : string.Empty,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var updatedAt) ? updatedAt.S ?? string.Empty : string.Empty
        };
    }
}
