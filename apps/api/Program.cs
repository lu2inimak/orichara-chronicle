using Api.Application.Auth;
using Api.Application.Common;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Presentation.Endpoints;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE") ?? string.Empty;
var region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";
var endpoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");
var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
var dynamoMode = Environment.GetEnvironmentVariable("DYNAMO_MODE");

builder.Services.AddSingleton(new DynamoOptions(tableName));
builder.Services.AddDynamoDb(region, endpoint, accessKey, secretKey, dynamoMode, tableName);

builder.Services.AddSingleton<IAuthenticator, MockAuthenticator>();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddUserModule();
builder.Services.AddCharactersModule();
builder.Services.AddWorldModule();
builder.Services.AddActivityModule();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(requestId))
    {
        requestId = "req_" + Guid.NewGuid().ToString("N");
    }

    RequestContext.RequestId = requestId;
    context.Response.Headers["X-Request-Id"] = requestId;

    var sw = System.Diagnostics.Stopwatch.StartNew();
    logger.LogInformation("event=http.request.start request_id={RequestId} method={Method} path={Path}", requestId, context.Request.Method, context.Request.Path);
    try
    {
        await next();
    }
    finally
    {
        sw.Stop();
        var errorCode = context.Response.Headers["X-Error-Code"].FirstOrDefault() ?? string.Empty;
        logger.LogInformation("event=http.request.end request_id={RequestId} status={Status} latency_ms={LatencyMs} error_code={ErrorCode}",
            requestId, context.Response.StatusCode, sw.ElapsedMilliseconds, errorCode);
        logger.LogInformation("metric=http.latency_ms request_id={RequestId} value={LatencyMs} method={Method} path={Path} status={Status}",
            requestId, sw.ElapsedMilliseconds, context.Request.Method, context.Request.Path, context.Response.StatusCode);
        if (context.Response.StatusCode >= 400)
        {
            logger.LogInformation("metric=http.error request_id={RequestId} value=1 status={Status} path={Path} error_code={ErrorCode}",
                requestId, context.Response.StatusCode, context.Request.Path, errorCode);
        }
    }
});

app.MapGet("/health", () => Results.Text("ok"));

app.MapUserEndpoints();
app.MapCharactersEndpoints();
app.MapWorldEndpoints();
app.MapActivityEndpoints();

app.Run();
