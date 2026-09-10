namespace Forum.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "forum-api";
    public string Audience { get; set; } = "forum-web";

    /// <summary>
    /// Read from configuration. The committed value is a development key so the project
    /// runs from a clone with no setup; in any real deployment this comes from an
    /// environment variable or secret store and never from a file in source control.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeHours { get; set; } = 8;
}
