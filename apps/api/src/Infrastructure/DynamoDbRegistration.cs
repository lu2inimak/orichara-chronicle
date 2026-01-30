using Amazon.DynamoDBv2;
using Amazon.Runtime;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Infrastructure;

public static class DynamoDbRegistration
{
    public static IServiceCollection AddDynamoDb(this IServiceCollection services, string region, string? endpoint, string? accessKey, string? secretKey, string? mode, string tableName)
    {
        if (string.Equals(mode, "memory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IDynamoDbClient>(new InMemoryDynamoDbClient(tableName));
            return services;
        }

        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
            config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            config.AuthenticationRegion = region;
        }

        IAmazonDynamoDB client;
        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            client = new AmazonDynamoDBClient(new BasicAWSCredentials(accessKey, secretKey), config);
        }
        else
        {
            client = new AmazonDynamoDBClient(config);
        }

        services.AddSingleton<IAmazonDynamoDB>(client);
        services.AddSingleton<IDynamoDbClient, DynamoDbClientWrapper>();
        return services;
    }
}
