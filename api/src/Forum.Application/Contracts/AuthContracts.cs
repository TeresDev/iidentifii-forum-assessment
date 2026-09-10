using System.ComponentModel.DataAnnotations;
using Forum.Domain.Enums;

namespace Forum.Application.Contracts;

// Validation attributes sit on the constructor parameters, not behind [property:].
// .NET 10 throws if validation metadata lands on the generated property of a record
// primary constructor, because it would never be evaluated.
public record RegisterRequest(
    [Required][StringLength(32, MinimumLength = 3)] string Username,
    [Required][StringLength(128, MinimumLength = 8)] string Password);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record UserSummary(Guid Id, string Username, UserRole Role);

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserSummary User);

public record AccessToken(string Value, DateTime ExpiresAtUtc);
