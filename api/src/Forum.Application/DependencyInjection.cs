using Forum.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PostService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
