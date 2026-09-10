namespace Forum.Api.Auth;

public static class AuthPolicies
{
    /// <summary>
    /// One named policy applied at the endpoint, rather than role-string checks scattered
    /// through handlers. There is a single place to read, and a single place to change.
    /// </summary>
    public const string Moderator = nameof(Moderator);
}
