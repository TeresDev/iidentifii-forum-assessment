using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forum.Application.Contracts;
using Forum.Infrastructure.Persistence.Seeding;

namespace Forum.IntegrationTests;

public class AuthEndpointTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    private HttpClient Client() => factory.CreateClient();

    [Fact]
    public async Task Login_succeeds_with_seeded_credentials()
    {
        var response = await Client().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("alice", SeedData.DefaultPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonDefaults.Options);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Role_is_serialised_as_a_string_not_a_number()
    {
        var response = await Client().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("mod.jordan", SeedData.ModeratorPassword));

        var raw = await response.Content.ReadAsStringAsync();

        // Asserting the deserialised enum would pass whether the wire value is
        // "Moderator" or 1. The client compares against the string, so the raw payload
        // is the only thing that proves the contract holds.
        Assert.Contains("\"role\":\"Moderator\"", raw);

        using var document = JsonDocument.Parse(raw);
        var role = document.RootElement.GetProperty("user").GetProperty("role");
        Assert.Equal(JsonValueKind.String, role.ValueKind);
    }

    [Fact]
    public async Task Unknown_username_and_wrong_password_are_indistinguishable()
    {
        var client = Client();

        var unknownUser = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest("no-such-person", "Password123!"));
        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest("alice", "definitely-wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        // traceId is per-request by design, so compare everything else. If the two
        // responses differed in title, code or status, the endpoint would be telling an
        // attacker which usernames exist.
        Assert.Equal(
            await WithoutTraceId(unknownUser),
            await WithoutTraceId(wrongPassword));
    }

    private static async Task<string> WithoutTraceId(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var fields = document.RootElement.EnumerateObject()
            .Where(p => p.Name != "traceId")
            .Select(p => $"{p.Name}={p.Value}");

        return string.Join('|', fields);
    }

    [Fact]
    public async Task Registration_rejects_a_taken_username_with_a_machine_readable_code()
    {
        var response = await Client().PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("alice", "Password123!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("\"code\":\"username-taken\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Registration_rejects_a_short_password_with_validation_details()
    {
        var response = await Client().PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("brand-new-user", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Password", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_registered_user_can_log_in()
    {
        var client = Client();
        var username = $"newcomer-{Guid.NewGuid():N}"[..20];

        var registered = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(username, "Password123!"));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var loggedIn = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(username, "Password123!"));

        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
    }

    [Fact]
    public async Task Registration_never_grants_the_moderator_role()
    {
        var client = Client();
        var username = $"aspiring-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(username, "Password123!"));

        var created = await response.Content.ReadFromJsonAsync<UserSummary>(JsonDefaults.Options);
        Assert.Equal(Domain.Enums.UserRole.User, created!.Role);
    }
}
