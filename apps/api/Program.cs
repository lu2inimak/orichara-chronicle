using Api.Shared.Auth;
using Api.Shared.Infrastructure;
using Api.Modules.User.Endpoints;
using Api.Modules.Characters.Endpoints;
using Api.Modules.World.Endpoints;
using Api.Modules.Activity.Endpoints;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.DynamoDBv2;

var builder = WebApplication.CreateBuilder(args);

var tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE") ?? string.Empty;
var region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";
var endpoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");
var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

builder.Services.AddSingleton(new DynamoOptions(tableName));

var dynamoConfig = new AmazonDynamoDBConfig
{
    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
};
if (!string.IsNullOrWhiteSpace(endpoint))
{
    dynamoConfig.ServiceURL = endpoint;
    dynamoConfig.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    dynamoConfig.AuthenticationRegion = region;
}

IAmazonDynamoDB dynamoClient;
if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
{
    var credentials = new BasicAWSCredentials(accessKey, secretKey);
    dynamoClient = new AmazonDynamoDBClient(credentials, dynamoConfig);
}
else
{
    dynamoClient = new AmazonDynamoDBClient(dynamoConfig);
}

builder.Services.AddSingleton(dynamoClient);

builder.Services.AddSingleton<IAuthenticator, MockAuthenticator>();

builder.Services.AddUserModule();
builder.Services.AddCharactersModule();
builder.Services.AddWorldModule();
builder.Services.AddActivityModule();

var app = builder.Build();

app.MapGet("/health", () => Results.Text("ok"));
app.MapGet("/aws-check", async (IAmazonDynamoDB dynamo, CancellationToken ct) =>
{
    try
    {
        var tables = await dynamo.ListTablesAsync(new ListTablesRequest(), ct);
        var names = tables.TableNames.Count == 0
            ? "[]"
            : $"[{string.Join(", ", tables.TableNames)}]";
        return Results.Text($"Dynamo err: <nil>\nDynamo tables: {names}\n");
    }
    catch (Exception ex)
    {
        return Results.Text($"Dynamo err: {ex.Message}\n");
    }
});

app.MapUserEndpoints();
app.MapCharactersEndpoints();
app.MapWorldEndpoints();
app.MapActivityEndpoints();

app.Run();
