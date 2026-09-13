namespace Infrastructure.Data;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public AdminSeed? Admin { get; init; }

    public sealed class AdminSeed
    {
        public string UserName { get; init; } = "admin";
        public string Email { get; init; } = "admin@localhost";
        public string Password { get; init; } = string.Empty;
    }
}
