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

public static class UserEndpoints
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        services.AddScoped<GetMeUsecase>();
        services.AddScoped<IUserRepository, DynamoUserRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (HttpContext context, GetMeUsecase usecase, CancellationToken ct) =>
        {
            try
            {
                var me = await usecase.ExecuteAsync(new GetMeRequest(ReadAuthToken(context)), ct);
                return ApiResults.Ok(context, me);
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
            catch (Exception ex)
            {
                return ApiResults.Error(context, new ApiError
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to read user profile.",
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
