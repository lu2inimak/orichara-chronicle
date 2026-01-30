using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Repositories;
using Api.Infrastructure.Repositories;
using Api.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Presentation.Endpoints;

public static class WorldEndpoints
{
    public static IServiceCollection AddWorldModule(this IServiceCollection services)
    {
        services.AddScoped<CreateWorldUsecase>();
        services.AddScoped<RequestJoinWorldUsecase>();
        services.AddScoped<ApproveAffiliationUsecase>();
        services.AddScoped<GetWorldTimelineUsecase>();
        services.AddScoped<IAffiliationRepository, DynamoWorldRepository>();
        services.AddScoped<IWorldRepository, DynamoWorldRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapWorldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/worlds", async (HttpContext context, CreateWorldUsecase usecase, WorldCreateRequest request, CancellationToken ct) =>
        {
            try
            {
                var created = await usecase.ExecuteAsync(new CreateWorldRequest(
                    ReadAuthToken(context),
                    request.Name,
                    request.Description
                ), ct);
                return ApiResults.Ok(context, created, StatusCodes.Status201Created);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("auth_required"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }
            catch (ArgumentException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "name is required.",
                    Details = new Dictionary<string, object> { ["field"] = "name" }
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create world.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapPost("/worlds/{worldId}/join", async (HttpContext context, string worldId, RequestJoinWorldUsecase usecase, JoinWorldRequest request, CancellationToken ct) =>
        {
            try
            {
                var created = await usecase.ExecuteAsync(new RequestJoinWorldRequest(
                    ReadAuthToken(context),
                    worldId,
                    request.CharacterId
                ), ct);
                return ApiResults.Ok(context, created, StatusCodes.Status201Created);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("auth_required"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("character_id"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "character_id is required.",
                    Details = new Dictionary<string, object> { ["field"] = "character_id" }
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("world_not_found"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "World not found."
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("character_not_found"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "Character not found."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "OWNERSHIP_MISMATCH",
                    Message = "You do not own this character."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create affiliation.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapPatch("/affiliations/{affiliationId}/approve", async (HttpContext context, string affiliationId, ApproveAffiliationUsecase usecase, CancellationToken ct) =>
        {
            try
            {
                var updated = await usecase.ExecuteAsync(new ApproveAffiliationRequest(
                    ReadAuthToken(context),
                    affiliationId
                ), ct);
                return ApiResults.Ok(context, updated);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("auth_required"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("affiliation_not_found"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "Affiliation not found."
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("world_not_found"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "World not found."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "FORBIDDEN",
                    Message = "Only host can approve affiliations."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update affiliation.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapGet("/worlds/{worldId}/timeline", async (HttpContext context, string worldId, GetWorldTimelineUsecase usecase, int? limit, CancellationToken ct) =>
        {
            try
            {
                var items = await usecase.ExecuteAsync(new GetWorldTimelineRequest(
                    ReadAuthToken(context),
                    worldId,
                    limit ?? 50
                ), ct);
                return ApiResults.Ok(context, new { items });
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("auth_required"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }
            catch (ArgumentException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "VALIDATION_FAILED",
                    Message = "world id is required."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to read timeline.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        return app;
    }

    private static string? ReadAuthToken(HttpContext context)
    {
        var auth = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(auth))
        {
            return auth;
        }
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}

public sealed class WorldCreateRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class JoinWorldRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("character_id")]
    public string CharacterId { get; set; } = string.Empty;
}
