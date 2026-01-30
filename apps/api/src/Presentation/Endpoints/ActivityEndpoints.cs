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

public static class ActivityEndpoints
{
    public static IServiceCollection AddActivityModule(this IServiceCollection services)
    {
        services.AddScoped<PostActivityUsecase>();
        services.AddScoped<SignActivityUsecase>();
        services.AddScoped<IAffiliationRepository, DynamoWorldRepository>();
        services.AddScoped<IActivityRepository, DynamoActivityRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/activities", async (HttpContext context, PostActivityUsecase usecase, ActivityCreateRequest request, CancellationToken ct) =>
        {
            try
            {
                var created = await usecase.ExecuteAsync(new PostActivityRequest(
                    ReadAuthToken(context),
                    request.AffiliationId,
                    request.Content,
                    request.CoCreators
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
            catch (ArgumentException ex) when (ex.Message.Contains("affiliation_id"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "affiliation_id is required.",
                    Details = new Dictionary<string, object> { ["field"] = "affiliation_id" }
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("content"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "content is required.",
                    Details = new Dictionary<string, object> { ["field"] = "content" }
                });
            }
            catch (KeyNotFoundException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "Affiliation not found."
                });
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("affiliation_not_active"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "AFFILIATION_NOT_ACTIVE",
                    Message = "Affiliation is not active."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "OWNERSHIP_MISMATCH",
                    Message = "You do not own this affiliation."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create activity.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapPost("/activities/{id}/sign", async (HttpContext context, string id, SignActivityUsecase usecase, ActivitySignRequest request, CancellationToken ct) =>
        {
            try
            {
                var updated = await usecase.ExecuteAsync(new SignActivityRequest(
                    ReadAuthToken(context),
                    id,
                    request.AffiliationId
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
            catch (ArgumentException ex) when (ex.Message.Contains("affiliation_id"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "MISSING_FIELD",
                    Message = "affiliation_id is required.",
                    Details = new Dictionary<string, object> { ["field"] = "affiliation_id" }
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("activity_not_found"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status404NotFound,
                    Code = "NOT_FOUND",
                    Message = "Activity not found."
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("signer_not_in_cocreators"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "FORBIDDEN",
                    Message = "Signer is not in co-creators."
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already_signed"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status409Conflict,
                    Code = "MULTISIG_ALREADY_SIGNED",
                    Message = "Already signed."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status403Forbidden,
                    Code = "OWNERSHIP_MISMATCH",
                    Message = "You do not own this affiliation."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to sign activity.",
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

public sealed class ActivityCreateRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("affiliation_id")]
    public string AffiliationId { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("co_creators")]
    public List<string> CoCreators { get; set; } = new();
}

public sealed class ActivitySignRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("affiliation_id")]
    public string AffiliationId { get; set; } = string.Empty;
}
