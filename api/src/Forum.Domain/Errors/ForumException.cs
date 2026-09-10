namespace Forum.Domain.Errors;

/// <summary>
/// Carries a stable machine-readable code rather than an HTTP status, so the domain
/// stays free of transport concerns. The API layer maps codes to status codes.
/// </summary>
public abstract class ForumException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
