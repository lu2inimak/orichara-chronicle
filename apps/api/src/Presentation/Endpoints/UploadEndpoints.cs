using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Api.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Presentation.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/uploads/presign", async (HttpContext context, PresignUploadRequest request, IAmazonS3 s3) =>
        {
            var bucket = Environment.GetEnvironmentVariable("S3_BUCKET") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(bucket))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "S3_BUCKET_MISSING",
                    Message = "S3_BUCKET is not configured."
                });
            }

            var fileName = Path.GetFileName(request.FileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "file_name is required."
                });
            }

            var prefix = string.IsNullOrWhiteSpace(request.Prefix) ? "uploads" : request.Prefix.Trim('/');
            var key = $"{prefix}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{fileName}";
            var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType;

            var presignRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(10),
                ContentType = contentType
            };

            var presignEndpoint = Environment.GetEnvironmentVariable("S3_PRESIGN_ENDPOINT");
            var url = string.IsNullOrWhiteSpace(presignEndpoint)
                ? s3.GetPreSignedURL(presignRequest)
                : BuildPresignedUrl(presignRequest, presignEndpoint);
            var publicBase = Environment.GetEnvironmentVariable("S3_PUBLIC_BASE_URL") ?? string.Empty;
            var publicUrl = string.IsNullOrWhiteSpace(publicBase)
                ? null
                : $"{publicBase.TrimEnd('/')}/{key}";

            return ApiResults.Ok(context, new
            {
                upload_url = url,
                public_url = publicUrl,
                key
            });
        });

        return app;
    }

    private static string BuildPresignedUrl(GetPreSignedUrlRequest request, string endpoint)
    {
        var region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region),
            ServiceURL = endpoint,
            ForcePathStyle = true,
            UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };
        IAmazonS3 client = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config)
            : new AmazonS3Client(config);
        return client.GetPreSignedURL(request);
    }
}

public sealed class PresignUploadRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("prefix")]
    public string? Prefix { get; set; }
}
