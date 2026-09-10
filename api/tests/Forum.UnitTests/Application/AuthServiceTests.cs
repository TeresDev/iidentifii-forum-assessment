using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Forum.Domain.Entities;
using Forum.Domain.Enums;
using Forum.Domain.Errors;
using Moq;

namespace Forum.UnitTests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private AuthService Sut() => new(_users.Object, _hasher.Object, _tokens.Object, TimeProvider.System);

    private static User Existing(string username = "alice") => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        PasswordHash = "stored-hash",
        Role = UserRole.User
    };

    [Fact]
    public async Task Login_verifies_a_hash_even_when_the_username_is_unknown()
    {
        _users.Setup(u => u.FindByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Sut().LoginAsync(new LoginRequest("ghost", "whatever"), default));

        // Returning early without hashing would make the unknown-user path finish in
        // microseconds while a real user costs ~100ms of PBKDF2. That difference is
        // measurable over the network and tells an attacker which usernames exist.
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        var user = Existing();
        _users.Setup(u => u.FindByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", "wrong")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Sut().LoginAsync(new LoginRequest(user.Username, "wrong"), default));
    }

    [Fact]
    public async Task Both_login_failures_report_the_same_message()
    {
        _users.Setup(u => u.FindByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var user = Existing();
        _users.Setup(u => u.FindByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", "wrong")).Returns(false);

        var unknownUser = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Sut().LoginAsync(new LoginRequest("ghost", "whatever"), default));
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Sut().LoginAsync(new LoginRequest(user.Username, "wrong"), default));

        Assert.Equal(unknownUser.Message, wrongPassword.Message);
        Assert.Equal(unknownUser.Code, wrongPassword.Code);
    }

    [Fact]
    public async Task Login_issues_a_token_for_valid_credentials()
    {
        var user = Existing();
        var expiry = DateTime.UtcNow.AddHours(8);
        _users.Setup(u => u.FindByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", "right")).Returns(true);
        _tokens.Setup(t => t.Issue(user)).Returns(new AccessToken("jwt-value", expiry));

        var response = await Sut().LoginAsync(new LoginRequest(user.Username, "right"), default);

        Assert.Equal("jwt-value", response.AccessToken);
        Assert.Equal(expiry, response.ExpiresAtUtc);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(UserRole.User, response.User.Role);
    }

    [Fact]
    public async Task Register_rejects_a_username_already_taken()
    {
        _users.Setup(u => u.UsernameExistsAsync("alice", It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        await Assert.ThrowsAsync<UsernameTakenException>(
            () => Sut().RegisterAsync(new RegisterRequest("alice", "Password123!"), default));

        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_creates_a_regular_user_never_a_moderator()
    {
        User? added = null;
        _users.Setup(u => u.UsernameExistsAsync("newcomer", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Password123!")).Returns("hashed");
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((u, _) => added = u)
              .Returns(Task.CompletedTask);

        var summary = await Sut().RegisterAsync(new RegisterRequest("newcomer", "Password123!"), default);

        Assert.NotNull(added);
        Assert.Equal("hashed", added!.PasswordHash);
        // Role is never taken from the request. Accepting it would let anyone register
        // themselves as a moderator and tag content.
        Assert.Equal(UserRole.User, added.Role);
        Assert.Equal(UserRole.User, summary.Role);
    }
}
