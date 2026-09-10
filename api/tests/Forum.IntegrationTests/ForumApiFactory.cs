using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Forum.IntegrationTests;

/// <summary>
/// Boots the real application pipeline against a private in-memory SQLite database.
/// Each factory instance holds its own connection, so tests cannot see one another's data.
/// </summary>
public class ForumApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public Task InitializeAsync() => _connection.OpenAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ForumDbContext>>();
            services.RemoveAll<ForumDbContext>();

            services.AddDbContext<ForumDbContext>(options => options.UseSqlite(_connection));
        });
    }
}
