using Amazon.Runtime;
using Amazon.S3;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Infrastructure;

public static class S3Registration
{
    public static IServiceCollection AddS3(this IServiceCollection services, string region, string? endpoint, string? accessKey, string? secretKey)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
            config.ForcePathStyle = true;
            config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }

        IAmazonS3 client;
        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
        }
        else
        {
            client = new AmazonS3Client(config);
        }

        services.AddSingleton<IAmazonS3>(client);
        return services;
    }
}
