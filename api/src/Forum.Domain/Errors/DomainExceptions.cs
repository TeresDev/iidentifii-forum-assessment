namespace Forum.Domain.Errors;

public sealed class SelfLikeException()
    : ForumException(ErrorCodes.SelfLike, "You cannot like your own post.");

public sealed class DuplicateLikeException()
    : ForumException(ErrorCodes.DuplicateLike, "You have already liked this post.");

public sealed class UsernameTakenException()
    : ForumException(ErrorCodes.UsernameTaken, "That username is already taken.");

public sealed class AlreadyTaggedException()
    : ForumException(ErrorCodes.AlreadyTagged, "That tag is already applied to this post.");

// Deliberately does not say which of the two was wrong. Disclosing that a username
// exists turns the login form into an account enumeration oracle.
public sealed class InvalidCredentialsException()
    : ForumException(ErrorCodes.InvalidCredentials, "Invalid username or password.");

public sealed class NotFoundException(string what)
    : ForumException(ErrorCodes.NotFound, $"{what} was not found.");
