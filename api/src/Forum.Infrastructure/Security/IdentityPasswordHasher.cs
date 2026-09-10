using Forum.Application.Abstractions;
using Forum.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Forum.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2 hasher. Only the hashing primitive is borrowed —
/// none of Identity's stores, managers or tables are used, so the authentication logic
/// stays in this codebase where it can be read and reasoned about.
/// </summary>
public class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly User Placeholder = new() { Username = string.Empty, PasswordHash = string.Empty };

    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(Placeholder, hash, password) != PasswordVerificationResult.Failed;
}
