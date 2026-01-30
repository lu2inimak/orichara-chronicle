using Api.Modules.Activity.Service;
using Api.Modules.Activity.Store;
using Api.Shared.Auth;
using Api.Shared.Http;

namespace Api.Modules.Activity.Endpoints;

public static class ActivityEndpoints
{
    public static IServiceCollection AddActivityModule(this IServiceCollection services)
    {
        services.AddScoped<ActivityService>();
        services.AddScoped<IActivityStore, DynamoActivityStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/activities", async (HttpContext context, ActivityService service, IAuthenticator auth, ActivityCreateRequest request, CancellationToken ct) =>
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
                var created = await service.PostActivityAsync(authInfo.UserId, request.AffiliationId, request.Content, request.CoCreators, ct);
                return ApiResults.Ok(context, created, StatusCodes.Status201Created);
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

        app.MapPost("/activities/{id}/sign", async (HttpContext context, string id, ActivityService service, IAuthenticator auth, ActivitySignRequest request, CancellationToken ct) =>
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
                var updated = await service.SignActivityAsync(authInfo.UserId, id, request.AffiliationId, ct);
                return ApiResults.Ok(context, updated);
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
