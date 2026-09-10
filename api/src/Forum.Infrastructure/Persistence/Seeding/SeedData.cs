using Forum.Domain.Enums;

namespace Forum.Infrastructure.Persistence.Seeding;

/// <summary>
/// The single definition of the demo dataset. The README credentials table and the
/// Postman collection variables are copied from here — if this changes, those change.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Fixed rather than relative to run time, so the documented ?from= and ?to= examples
    /// return the same rows on every machine and never drift out of range.
    /// </summary>
    public static readonly DateTime AnchorUtc = new(2026, 03, 01, 09, 00, 00, DateTimeKind.Utc);

    public const string DefaultPassword = "Password123!";
    public const string ModeratorPassword = "Moderator1!";

    public const string MisleadingTag = "misleading-information";

    public static readonly (string Slug, string DisplayName)[] Tags =
    [
        (MisleadingTag, "Misleading or false information"),
        ("integration", "Integration"),
        ("authentication", "Authentication"),
        ("sdk", "SDK"),
        ("best-practices", "Best practices")
    ];

    /// <summary>Index 0 is the moderator. Post counts give an uneven author distribution.</summary>
    public static readonly (string Username, UserRole Role, int PostCount)[] Users =
    [
        ("mod.jordan", UserRole.Moderator, 0),
        ("alice", UserRole.User, 11),
        ("ben", UserRole.User, 6),
        ("chi", UserRole.User, 4),
        ("dana", UserRole.User, 2),
        ("eli", UserRole.User, 1),
        ("fay", UserRole.User, 1),
        ("gus", UserRole.User, 1)
    ];

    /// <summary>
    /// Deliberate ties. Sorting by like count without a tiebreaker would let these rows
    /// repeat and vanish across pages, so the seed makes that failure reproducible.
    /// </summary>
    public static readonly int[] LikeCounts =
        [7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 3, 3, 3, 2, 2, 2, 2, 1, 1, 1, 0, 0, 0, 0, 0, 0];

    /// <summary>
    /// One post with 13 comments exercises comment paging at the default page size of 10.
    /// Nine posts with none exercise the empty state.
    /// </summary>
    public static readonly int[] CommentCounts =
        [13, 5, 4, 3, 3, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    /// <summary>Post indexes the moderator has flagged.</summary>
    public static readonly int[] FlaggedPostIndexes = [3, 12];

    public static readonly string[] PostTitles =
    [
        "Handling liveness check timeouts on slow connections",
        "Best practice for storing verification session IDs",
        "Webhook retries are firing twice for the same event",
        "Guaranteed 100% match rate on every document type",
        "SDK 4.2 breaks the Android camera preview",
        "How do you handle partial OCR results?",
        "Rate limits on the verification endpoint",
        "Sandbox returns different confidence scores than production",
        "Migrating from v1 to v2 of the results API",
        "Passport MRZ parsing fails for some issuing countries",
        "Recommended retry strategy for network failures",
        "Session expiry: client-side versus server-side",
        "You can skip document verification entirely with this header",
        "Bulk verification: is there a batch endpoint?",
        "Selfie capture quality thresholds",
        "Callback signature verification example in C#",
        "Handling users who fail verification three times",
        "Testing against sandbox without burning quota",
        "Localisation support for the hosted flow",
        "Audit log retention period",
        "iOS SDK bundle size after 4.2",
        "Detecting screen-replay attacks",
        "Which fields are returned for a failed check?",
        "Timeouts when uploading large document images",
        "Sandbox credentials rotating unexpectedly",
        "Feature request: webhook delivery dashboard"
    ];
}
