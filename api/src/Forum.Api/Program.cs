using Forum.Infrastructure;
using Forum.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Migrate and seed on startup so the assessor needs no database step beyond `dotnet run`.
await app.Services.SeedForumDatabaseAsync();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
