using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Repositories;

public class UserRepository(ForumDbContext db) : IUserRepository
{
    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Username == username, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct) =>
        await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserSummary(u.Id, u.Username, u.Role))
            .ToListAsync(ct);
}
