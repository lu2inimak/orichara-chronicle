using Api.Application.DTO;
using Api.Application.Usecase;
using Api.Domain.Repositories;
using Api.Infrastructure.Repositories;
using Api.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Api.Presentation.Endpoints;

public static class CharacterEndpoints
{
    public static IServiceCollection AddCharactersModule(this IServiceCollection services)
    {
        services.AddScoped<CreateCharacterUsecase>();
        services.AddScoped<UpdateCharacterUsecase>();
        services.AddScoped<ICharacterRepository, DynamoCharacterRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapCharactersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/characters", async (HttpContext context, ICharacterRepository repository, int? limit, CancellationToken ct) =>
        {
            try
            {
                var items = await repository.ListCharactersAsync(limit ?? 50, ct);
                var responseItems = items.Select(character => new CharacterListItem
                {
                    Id = character.Id,
                    Name = character.Name,
                    Author = character.OwnerId,
                    AvatarUrl = character.AvatarUrl,
                    UpdatedAt = character.UpdatedAt
                }).ToList();

                return ApiResults.Ok(context, new { items = responseItems });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to list characters.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapPost("/characters", async (HttpContext context, CreateCharacterUsecase usecase, CharacterCreateRequest request, CancellationToken ct) =>
        {
            try
            {
                var created = await usecase.ExecuteAsync(new CreateCharacterRequest(
                    ReadAuthToken(context),
                    request.Name,
                    request.Bio,
                    request.AvatarUrl
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
            catch (ArgumentException ex) when (ex.Message.Contains("name"))
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
                    Message = "Failed to create character.",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        });

        app.MapPatch("/characters/{id}", async (HttpContext context, string id, UpdateCharacterUsecase usecase, CharacterPatchRequest request, CancellationToken ct) =>
        {
            var updates = new Dictionary<string, string>();
            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return ApiResults.Error(context, new ApiError
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Code = "VALIDATION_FAILED",
                        Message = "name cannot be empty.",
                        Details = new Dictionary<string, object> { ["field"] = "name" }
                    });
                }
                updates["Name"] = request.Name.Trim();
            }
            if (request.Bio != null)
            {
                updates["Bio"] = request.Bio.Trim();
            }
            if (request.AvatarUrl != null)
            {
                updates["AvatarURL"] = request.AvatarUrl.Trim();
            }

            try
            {
                var updated = await usecase.ExecuteAsync(new UpdateCharacterRequest(
                    ReadAuthToken(context),
                    id,
                    updates
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
            catch (KeyNotFoundException)
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
            catch (ArgumentException ex) when (ex.Message.Contains("no_updates"))
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "VALIDATION_FAILED",
                    Message = "No updates provided."
                });
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update character.",
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

public sealed class CharacterListItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class CharacterCreateRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("bio")]
    public string? Bio { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}

public sealed class CharacterPatchRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("bio")]
    public string? Bio { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}
