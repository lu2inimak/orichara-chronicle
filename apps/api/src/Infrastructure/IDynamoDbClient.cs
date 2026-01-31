using Amazon.DynamoDBv2.Model;

namespace Api.Infrastructure;

public interface IDynamoDbClient
{
    Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken);
    Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken);
    Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken);
    Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken);
    Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken);
    Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken);
    Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken);
    Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken);
}
