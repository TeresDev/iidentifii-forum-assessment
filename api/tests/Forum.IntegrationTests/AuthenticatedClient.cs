using System.Net.Http.Headers;
using System.Net.Http.Json;
using Forum.Application.Contracts;
using Forum.Infrastructure.Persistence.Seeding;

namespace Forum.IntegrationTests;

public static class AuthenticatedClient
{
    public static async Task<HttpClient> AsAsync(
        this ForumApiFactory factory,
        string username,
        string? password = null)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(username, password ?? DefaultPasswordFor(username)));

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonDefaults.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }

    private static string DefaultPasswordFor(string username) =>
        username == "mod.jordan" ? SeedData.ModeratorPassword : SeedData.DefaultPassword;
}
