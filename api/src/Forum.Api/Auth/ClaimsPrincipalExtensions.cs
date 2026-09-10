using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Forum.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Null for anonymous callers, which is a valid state on browse endpoints.</summary>
    public static Guid? UserIdOrNull(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out var id) ? id : null;
    }

    public static Guid UserId(this ClaimsPrincipal principal) =>
        principal.UserIdOrNull()
        ?? throw new InvalidOperationException("No authenticated user on this request.");
}
