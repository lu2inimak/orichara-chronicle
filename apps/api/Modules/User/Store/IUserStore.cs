using Api.Modules.User.Models;

namespace Api.Modules.User.Store;

public interface IUserStore
{
    Task<MeResponse> GetMeAsync(string userId, CancellationToken cancellationToken);
}
