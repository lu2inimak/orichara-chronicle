using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Api.Application.Common;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure;

public sealed class DynamoDbClientWrapper : IDynamoDbClient
{
    private readonly IAmazonDynamoDB _client;
    private readonly ILogger<DynamoDbClientWrapper> _logger;

    public DynamoDbClientWrapper(IAmazonDynamoDB client, ILogger<DynamoDbClientWrapper> logger)
    {
        _client = client;
        _logger = logger;
    }

    public Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("GetItem", request.TableName, () => _client.GetItemAsync(request, cancellationToken));

    public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("PutItem", request.TableName, () => _client.PutItemAsync(request, cancellationToken));

    public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("UpdateItem", request.TableName, () => _client.UpdateItemAsync(request, cancellationToken));

    public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("Query", request.TableName, () => _client.QueryAsync(request, cancellationToken));

    public Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("TransactWriteItems", request.TransactItems.FirstOrDefault()?.Put?.TableName, () => _client.TransactWriteItemsAsync(request, cancellationToken));

    public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("ListTables", null, () => _client.ListTablesAsync(request, cancellationToken));

    public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken) =>
        ExecuteWithLogging("DescribeTable", request.TableName, () => _client.DescribeTableAsync(request, cancellationToken));

    private async Task<T> ExecuteWithLogging<T>(string operation, string? table, Func<Task<T>> action)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            _logger.LogInformation("event=ddb.{Operation} request_id={RequestId} table={Table} status=ok latency_ms={LatencyMs}",
                operation, RequestContext.RequestId, table ?? string.Empty, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "event=ddb.{Operation} request_id={RequestId} table={Table} status=error latency_ms={LatencyMs}",
                operation, RequestContext.RequestId, table ?? string.Empty, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
