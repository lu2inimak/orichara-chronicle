using Api.Modules.User.Models;
using Api.Modules.User.Store;

namespace Api.Modules.User.Service;

public sealed class UserService
{
    private readonly IUserStore _store;

    public UserService(IUserStore store)
    {
        _store = store;
    }

    public Task<MeResponse> GetMeAsync(string userId, CancellationToken cancellationToken)
    {
        return _store.GetMeAsync(userId, cancellationToken);
    }
}
