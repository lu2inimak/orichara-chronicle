using Api.Domain.ReadModels;

namespace Api.Domain.Repositories;

public interface IUserRepository
{
    Task<UserSnapshot> GetSnapshotAsync(string userId, CancellationToken cancellationToken);
}
