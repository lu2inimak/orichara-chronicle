using Amazon.DynamoDBv2.Model;
using Api.Domain.Entities;
using Api.Domain.Repositories;
using Api.Infrastructure;

namespace Api.Infrastructure.Repositories;

public sealed class DynamoCharacterRepository : ICharacterRepository
{
    private readonly IDynamoDbClient _dynamo;
    private readonly DynamoOptions _options;

    public DynamoCharacterRepository(IDynamoDbClient dynamo, DynamoOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task<Character?> GetCharacterAsync(string characterId, CancellationToken cancellationToken)
    {
        EnsureTable();
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"CHAR#{characterId}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            }
        }, cancellationToken);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        return MapCharacter(response.Item, characterId);
    }

    public async Task<Character> CreateCharacterAsync(string userId, Character character, CancellationToken cancellationToken)
    {
        EnsureTable();

        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"CHAR#{character.Id}" },
            ["SK"] = new AttributeValue { S = "INFO" },
            ["OwnerID"] = new AttributeValue { S = userId },
            ["Name"] = new AttributeValue { S = character.Name },
            ["CreatedAt"] = new AttributeValue { S = character.CreatedAt },
            ["UpdatedAt"] = new AttributeValue { S = character.UpdatedAt }
        };
        if (!string.IsNullOrWhiteSpace(character.Bio))
        {
            item["Bio"] = new AttributeValue { S = character.Bio };
        }
        if (!string.IsNullOrWhiteSpace(character.AvatarUrl))
        {
            item["AvatarURL"] = new AttributeValue { S = character.AvatarUrl };
        }

        var pointer = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
            ["SK"] = new AttributeValue { S = $"OWN_CHAR#{character.Id}" },
            ["RefType"] = new AttributeValue { S = "Character" },
            ["RefID"] = new AttributeValue { S = character.Id }
        };

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new() { Put = new Put { TableName = _options.TableName, Item = item } },
                new() { Put = new Put { TableName = _options.TableName, Item = pointer } }
            }
        }, cancellationToken);

        character.OwnerId = userId;
        return character;
    }

    public async Task<Character> UpdateCharacterAsync(string characterId, Dictionary<string, string> updates, CancellationToken cancellationToken)
    {
        EnsureTable();
        if (updates.Count == 0)
        {
            throw new InvalidOperationException("no_updates");
        }

        var exprNames = new Dictionary<string, string>();
        var exprValues = new Dictionary<string, AttributeValue>();
        var setParts = new List<string>();

        foreach (var (key, value) in updates)
        {
            var nameKey = "#" + key;
            var valueKey = ":" + key;
            exprNames[nameKey] = key;
            exprValues[valueKey] = new AttributeValue { S = value };
            setParts.Add($"{nameKey} = {valueKey}");
        }

        exprNames["#UpdatedAt"] = "UpdatedAt";
        exprValues[":UpdatedAt"] = new AttributeValue { S = updates["UpdatedAt"] };
        setParts.Add("#UpdatedAt = :UpdatedAt");

        var response = await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"CHAR#{characterId}" },
                ["SK"] = new AttributeValue { S = "INFO" }
            },
            UpdateExpression = "SET " + string.Join(", ", setParts),
            ExpressionAttributeNames = exprNames,
            ExpressionAttributeValues = exprValues,
            ReturnValues = "ALL_NEW"
        }, cancellationToken);

        return MapCharacter(response.Attributes, characterId);
    }

    private void EnsureTable()
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("DDB table name is required");
        }
    }

    private static Character MapCharacter(Dictionary<string, AttributeValue> item, string characterId)
    {
        return new Character
        {
            Id = characterId,
            OwnerId = item.TryGetValue("OwnerID", out var owner) ? owner.S ?? string.Empty : string.Empty,
            Name = item.TryGetValue("Name", out var name) ? name.S ?? string.Empty : string.Empty,
            Bio = item.TryGetValue("Bio", out var bio) ? bio.S : null,
            AvatarUrl = item.TryGetValue("AvatarURL", out var avatar) ? avatar.S : null,
            CreatedAt = item.TryGetValue("CreatedAt", out var createdAt) ? createdAt.S ?? string.Empty : string.Empty,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var updatedAt) ? updatedAt.S ?? string.Empty : string.Empty
        };
    }
}
