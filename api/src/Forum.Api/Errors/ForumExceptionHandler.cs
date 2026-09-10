using Forum.Domain.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Errors;

/// <summary>
/// Translates domain error codes into HTTP status codes. The domain deliberately knows
/// nothing about HTTP, so this is the single place the two vocabularies meet.
/// </summary>
public class ForumExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        [ErrorCodes.InvalidCredentials] = StatusCodes.Status401Unauthorized,
        [ErrorCodes.Forbidden] = StatusCodes.Status403Forbidden,
        [ErrorCodes.NotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.UsernameTaken] = StatusCodes.Status409Conflict,
        [ErrorCodes.SelfLike] = StatusCodes.Status409Conflict,
        [ErrorCodes.DuplicateLike] = StatusCodes.Status409Conflict,
        [ErrorCodes.AlreadyTagged] = StatusCodes.Status409Conflict
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not ForumException forumException)
        {
            return false;
        }

        var status = StatusByCode.GetValueOrDefault(forumException.Code, StatusCodes.Status400BadRequest);
        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = forumException.Message,
                Extensions = { ["code"] = forumException.Code }
            }
        });
    }
}
