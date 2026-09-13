using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Data;

/// <summary>Creates the initial Admin user from the <c>Seed:Admin</c> config section if no users exist.</summary>
public sealed class DatabaseSeeder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly SeedSettings _settings;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        IOptions<SeedSettings> settings,
        ILogger<DatabaseSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = _settings.Admin;
        if (admin is null || string.IsNullOrWhiteSpace(admin.Password))
        {
            _logger.LogInformation("Seed:Admin:Password not configured; skipping admin seed.");
            return;
        }

        if (await _unitOfWork.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        await _unitOfWork.Users.AddAsync(new User
        {
            UserName = admin.UserName,
            Email = admin.Email,
            PasswordHash = _hasher.Hash(admin.Password),
            Role = Role.Admin,
            CreatedOn = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded admin user '{UserName}'.", admin.UserName);
    }
}
