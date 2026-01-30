using Api.Modules.User.Service;
using Api.Modules.User.Store;
using Api.Shared.Auth;
using Api.Shared.Http;

namespace Api.Modules.User.Endpoints;

public static class UserEndpoints
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        services.AddScoped<UserService>();
        services.AddScoped<IUserStore, DynamoUserStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (HttpContext context, UserService service, IAuthenticator auth, CancellationToken ct) =>
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
                var me = await service.GetMeAsync(authInfo.UserId, ct);
                return ApiResults.Ok(context, me);
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
}
