using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Api.Infrastructure;

public sealed class DynamoDbClientWrapper : IDynamoDbClient
{
    private readonly IAmazonDynamoDB _client;

    public DynamoDbClientWrapper(IAmazonDynamoDB client)
    {
        _client = client;
    }

    public Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken) =>
        _client.GetItemAsync(request, cancellationToken);

    public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken) =>
        _client.PutItemAsync(request, cancellationToken);

    public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken) =>
        _client.UpdateItemAsync(request, cancellationToken);

    public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken) =>
        _client.QueryAsync(request, cancellationToken);

    public Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken) =>
        _client.TransactWriteItemsAsync(request, cancellationToken);

    public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken) =>
        _client.ListTablesAsync(request, cancellationToken);

    public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken) =>
        _client.DescribeTableAsync(request, cancellationToken);
}
