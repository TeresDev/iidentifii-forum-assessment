using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Forum.Domain.Enums;
using Forum.Domain.Errors;

namespace Forum.Application.Services;

public class AuthService(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    TimeProvider clock)
{
    /// <summary>
    /// A real PBKDF2 hash of a value nobody knows. Verifying against this when the
    /// username is unknown keeps both failure paths on the same code path and the same
    /// cost, so response time does not reveal whether an account exists.
    /// </summary>
    private const string DummyHash =
        "AQAAAAIAAYagAAAAEJ8Xr1kK0mVQ3lZ7cV0uWQnGZ0mQe1v3pQ0YyN8hJ5xK9pQ2wR7tL4mN6cV8bX1aZg==";

    public async Task<UserSummary> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await users.UsernameExistsAsync(request.Username, ct))
        {
            throw new UsernameTakenException();
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = hasher.Hash(request.Password),
            // Never taken from the request. Accepting a role here would let anyone
            // register as a moderator and tag content.
            Role = UserRole.User,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };

        await users.AddAsync(user, ct);

        return new UserSummary(user.Id, user.Username, user.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByUsernameAsync(request.Username, ct);

        var passwordMatches = hasher.Verify(user?.PasswordHash ?? DummyHash, request.Password);

        if (user is null || !passwordMatches)
        {
            throw new InvalidCredentialsException();
        }

        var token = tokens.Issue(user);

        return new AuthResponse(
            token.Value,
            token.ExpiresAtUtc,
            new UserSummary(user.Id, user.Username, user.Role));
    }
}
