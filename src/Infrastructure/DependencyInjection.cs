using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Products);
        services.AddScoped<IItemRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Items);
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Users);
        services.AddScoped<IRefreshTokenRepository>(sp => sp.GetRequiredService<IUnitOfWork>().RefreshTokens);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
