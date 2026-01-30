using Api.Modules.Activity.Service;
using Api.Modules.World.Service;
using Api.Modules.World.Store;
using Api.Shared.Auth;
using Api.Shared.Http;

namespace Api.Modules.World.Endpoints;

public static class WorldEndpoints
{
    public static IServiceCollection AddWorldModule(this IServiceCollection services)
    {
        services.AddScoped<WorldService>();
        services.AddScoped<IWorldStore, DynamoWorldStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapWorldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/worlds", async (HttpContext context, WorldService service, IAuthenticator auth, WorldCreateRequest request, CancellationToken ct) =>
        {
            var authInfo = auth.Authenticate(context);
            if (authInfo is null)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }

            try
            {
                var created = await service.CreateWorldAsync(authInfo.UserId, request.Name, request.Description, ct);
                return ApiResults.Ok(context, created, StatusCodes.Status201Created);
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

        app.MapPost("/worlds/{worldId}/join", async (HttpContext context, string worldId, WorldService service, IAuthenticator auth, JoinWorldRequest request, CancellationToken ct) =>
        {
            var authInfo = auth.Authenticate(context);
            if (authInfo is null)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }

            try
            {
                var created = await service.RequestJoinAsync(authInfo.UserId, worldId, request.CharacterId, ct);
                return ApiResults.Ok(context, created, StatusCodes.Status201Created);
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

        app.MapPatch("/affiliations/{affiliationId}/approve", async (HttpContext context, string affiliationId, WorldService service, IAuthenticator auth, CancellationToken ct) =>
        {
            var authInfo = auth.Authenticate(context);
            if (authInfo is null)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }

            try
            {
                var updated = await service.ApproveAffiliationAsync(authInfo.UserId, affiliationId, ct);
                return ApiResults.Ok(context, updated);
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

        app.MapGet("/worlds/{worldId}/timeline", async (HttpContext context, string worldId, ActivityService activityService, IAuthenticator auth, int? limit, CancellationToken ct) =>
        {
            var authInfo = auth.Authenticate(context);
            if (authInfo is null)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required."
                });
            }

            try
            {
                var items = await activityService.GetTimelineAsync(worldId, limit ?? 50, ct);
                return ApiResults.Ok(context, new { items });
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
