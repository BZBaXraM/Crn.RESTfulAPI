using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.TestSupport;

/// <summary>
/// One open Sqlite in-memory connection per test class instance (xUnit creates a fresh instance per test),
/// so each test gets an isolated schema without needing a real SQL Server.
/// </summary>
public sealed class SqliteDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public SqliteDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
