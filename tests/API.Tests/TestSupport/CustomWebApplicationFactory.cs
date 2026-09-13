using System.Data.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.Tests.TestSupport;

/// <summary>
/// Boots the real API pipeline (auth, validation, error handling, versioning) but replaces the
/// SQL Server DbContext with a single open Sqlite in-memory connection so tests need no external
/// database or Docker. One factory instance = one isolated database.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection = new SqliteConnection("DataSource=:memory:");

    // Production wiring already registered SQL Server's provider services into the app's service
    // collection by the time this runs. EF Core refuses to have two providers' services in one
    // collection, so Sqlite gets its own dedicated internal service provider instead of sharing it.
    private readonly IServiceProvider _sqliteInternalServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // No signing-key override here on purpose: API.Extensions.ServiceCollectionExtensions reads
            // Jwt:SigningKey synchronously while building the host (for JwtBearer's TokenValidationParameters),
            // before ConfigureAppConfiguration overrides are visible to that code, while JwtTokenService reads
            // it lazily via IOptions<JwtSettings> at request time. Overriding it here would desync the two,
            // so tests rely on appsettings.json already carrying a valid (>= 32 char) key.
            // No seeded admin here either: tests register/login their own users so each test is self-contained.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Admin:Password"] = null
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddDbContext's TryAdd semantics mean simply re-adding it here would not fully replace
            // the SQL Server-flavoured DbContextOptions<ApplicationDbContext> already registered by
            // production startup, so both providers' options extensions end up on the same options
            // object. Registering the options and the context by hand guarantees a clean slate.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            _connection.Open();
            services.AddScoped(_ =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlite(_connection);
                optionsBuilder.UseInternalServiceProvider(_sqliteInternalServices);
                return optionsBuilder.Options;
            });
            services.AddScoped<ApplicationDbContext>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Inserts an Admin-role user directly through the DbContext (bypassing the public, User-only
    /// register endpoint) so tests can obtain a token for role-gated endpoints.
    /// </summary>
    public async Task CreateAdminAsync(string userName, string password)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        context.Users.Add(new User
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            PasswordHash = hasher.Hash(password),
            Role = Role.Admin,
            CreatedOn = clock.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
