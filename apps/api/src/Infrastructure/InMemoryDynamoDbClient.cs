using Amazon.DynamoDBv2.Model;

namespace Api.Infrastructure;

public sealed class InMemoryDynamoDbClient : IDynamoDbClient
{
    private readonly Dictionary<string, Dictionary<(string PK, string SK), Dictionary<string, AttributeValue>>> _tables = new();

    public InMemoryDynamoDbClient(string tableName)
    {
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            _tables[tableName] = new Dictionary<(string PK, string SK), Dictionary<string, AttributeValue>>();
        }
    }

    public Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken)
    {
        var table = GetTable(request.TableName);
        var key = GetKey(request.Key);
        table.TryGetValue(key, out var item);
        return Task.FromResult(new GetItemResponse { Item = item ?? new Dictionary<string, AttributeValue>() });
    }

    public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken)
    {
        var table = GetTable(request.TableName);
        var key = GetKey(request.Item);
        table[key] = new Dictionary<string, AttributeValue>(request.Item);
        return Task.FromResult(new PutItemResponse());
    }

    public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var table = GetTable(request.TableName);
        var key = GetKey(request.Key);
        if (!table.TryGetValue(key, out var item))
        {
            item = new Dictionary<string, AttributeValue>(request.Key);
            table[key] = item;
        }

        if (request.UpdateExpression != null && request.UpdateExpression.StartsWith("SET ", StringComparison.Ordinal))
        {
            var expr = request.UpdateExpression[4..];
            var parts = expr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var tokens = part.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                {
                    continue;
                }
                var nameToken = tokens[0];
                var valueToken = tokens[1];
                var attrName = ResolveName(nameToken, request.ExpressionAttributeNames);
                if (request.ExpressionAttributeValues != null && request.ExpressionAttributeValues.TryGetValue(valueToken, out var value))
                {
                    item[attrName] = value;
                }
            }
        }

        return Task.FromResult(new UpdateItemResponse
        {
            Attributes = string.Equals(request.ReturnValues, "ALL_NEW", StringComparison.Ordinal)
                ? new Dictionary<string, AttributeValue>(item)
                : new Dictionary<string, AttributeValue>()
        });
    }

    public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        var table = GetTable(request.TableName);
        var items = table.Values.ToList();
        if (request.KeyConditionExpression != null && request.ExpressionAttributeValues != null)
        {
            if (request.KeyConditionExpression.Contains("GSI_TimelinePK", StringComparison.Ordinal))
            {
                var pk = request.ExpressionAttributeValues[":pk"].S ?? string.Empty;
                items = items.Where(i => i.TryGetValue("GSI_TimelinePK", out var pkVal) && pkVal.S == pk).ToList();
                items = request.ScanIndexForward == false
                    ? items.OrderByDescending(i => i.TryGetValue("GSI_TimelineSK", out var sk) ? sk.S : string.Empty).ToList()
                    : items.OrderBy(i => i.TryGetValue("GSI_TimelineSK", out var sk) ? sk.S : string.Empty).ToList();
            }
            else if (request.KeyConditionExpression.Contains("PK = :pk", StringComparison.Ordinal))
            {
                var pk = request.ExpressionAttributeValues[":pk"].S ?? string.Empty;
                items = items.Where(i => i.TryGetValue("PK", out var pkVal) && pkVal.S == pk).ToList();
                items = items.OrderBy(i => i.TryGetValue("SK", out var sk) ? sk.S : string.Empty).ToList();
            }
        }

        if (request.Limit is > 0)
        {
            items = items.Take(request.Limit.Value).ToList();
        }

        return Task.FromResult(new QueryResponse { Items = items });
    }

    public async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken)
    {
        foreach (var item in request.TransactItems)
        {
            if (item.Put != null)
            {
                await PutItemAsync(new PutItemRequest { TableName = item.Put.TableName, Item = item.Put.Item }, cancellationToken);
            }
            if (item.Update != null)
            {
                await UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = item.Update.TableName,
                    Key = item.Update.Key,
                    UpdateExpression = item.Update.UpdateExpression,
                    ExpressionAttributeNames = item.Update.ExpressionAttributeNames,
                    ExpressionAttributeValues = item.Update.ExpressionAttributeValues
                }, cancellationToken);
            }
        }
        return new TransactWriteItemsResponse();
    }

    public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ListTablesResponse { TableNames = _tables.Keys.ToList() });
    }

    public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken)
    {
        if (!_tables.ContainsKey(request.TableName))
        {
            throw new ResourceNotFoundException("Cannot do operations on a non-existent table");
        }
        return Task.FromResult(new DescribeTableResponse
        {
            Table = new TableDescription { TableName = request.TableName, TableStatus = "ACTIVE" }
        });
    }

    private Dictionary<(string PK, string SK), Dictionary<string, AttributeValue>> GetTable(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ResourceNotFoundException("Cannot do operations on a non-existent table");
        }
        if (!_tables.TryGetValue(name, out var table))
        {
            throw new ResourceNotFoundException("Cannot do operations on a non-existent table");
        }
        return table;
    }

    private static (string PK, string SK) GetKey(Dictionary<string, AttributeValue> map)
    {
        var pk = map.TryGetValue("PK", out var pkValue) ? pkValue.S ?? string.Empty : string.Empty;
        var sk = map.TryGetValue("SK", out var skValue) ? skValue.S ?? string.Empty : string.Empty;
        return (pk, sk);
    }

    private static string ResolveName(string nameToken, Dictionary<string, string>? names)
    {
        if (nameToken.StartsWith("#", StringComparison.Ordinal) && names != null && names.TryGetValue(nameToken, out var mapped))
        {
            return mapped;
        }
        return nameToken.TrimStart('#');
    }
}
