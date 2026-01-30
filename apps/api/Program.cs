using Api.Application.Auth;
using Api.Infrastructure;
using Api.Infrastructure.Auth;
using Api.Presentation.Endpoints;
using Microsoft.AspNetCore.Http.Json;
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

app.MapGet("/health", () => Results.Text("ok"));

app.MapUserEndpoints();
app.MapCharactersEndpoints();
app.MapWorldEndpoints();
app.MapActivityEndpoints();

app.Run();
