using Forum.Domain.Entities;
using Forum.Domain.Errors;

namespace Forum.UnitTests.Domain;

public class PostLikeGuardTests
{
    private static Post PostBy(Guid authorId) => new()
    {
        Id = Guid.NewGuid(),
        AuthorId = authorId,
        Title = "A title",
        Body = "A body"
    };

    [Fact]
    public void EnsureCanBeLikedBy_throws_when_the_author_likes_their_own_post()
    {
        var authorId = Guid.NewGuid();
        var post = PostBy(authorId);

        var ex = Assert.Throws<SelfLikeException>(() => post.EnsureCanBeLikedBy(authorId));

        Assert.Equal(ErrorCodes.SelfLike, ex.Code);
    }

    [Fact]
    public void EnsureCanBeLikedBy_allows_a_different_user()
    {
        var post = PostBy(Guid.NewGuid());

        post.EnsureCanBeLikedBy(Guid.NewGuid());
    }

    [Fact]
    public void SelfLike_message_does_not_disclose_the_author()
    {
        var authorId = Guid.NewGuid();
        var post = PostBy(authorId);

        var ex = Assert.Throws<SelfLikeException>(() => post.EnsureCanBeLikedBy(authorId));

        Assert.DoesNotContain(authorId.ToString(), ex.Message);
    }
}
