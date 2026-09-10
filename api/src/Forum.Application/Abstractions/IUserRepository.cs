using Forum.Application.Contracts;
using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);

    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);

    Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct);
}
