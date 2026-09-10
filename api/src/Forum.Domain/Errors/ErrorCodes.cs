namespace Forum.Domain.Errors;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation-failed";
    public const string InvalidCredentials = "invalid-credentials";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not-found";
    public const string UsernameTaken = "username-taken";
    public const string SelfLike = "self-like";
    public const string DuplicateLike = "duplicate-like";
    public const string AlreadyTagged = "already-tagged";
}
