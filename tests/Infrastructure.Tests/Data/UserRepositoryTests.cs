using Domain.Entities;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.TestSupport;
using Xunit;

namespace Infrastructure.Tests.Data;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        _sut = new UserRepository(_fixture.Context);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UserNameExistsAsync_and_EmailExistsAsync_reflect_persisted_users()
    {
        await _sut.AddAsync(new User { UserName = "alice", Email = "alice@example.com", PasswordHash = "h", CreatedOn = DateTime.UtcNow });
        await _fixture.Context.SaveChangesAsync();

        Assert.True(await _sut.UserNameExistsAsync("alice"));
        Assert.True(await _sut.EmailExistsAsync("alice@example.com"));
        Assert.False(await _sut.UserNameExistsAsync("bob"));
        Assert.False(await _sut.EmailExistsAsync("bob@example.com"));
    }

    [Fact]
    public async Task GetByUserNameAsync_returns_null_when_not_found()
        => Assert.Null(await _sut.GetByUserNameAsync("ghost"));

    [Fact]
    public async Task AnyAsync_is_false_before_any_user_and_true_after()
    {
        Assert.False(await _sut.AnyAsync());

        await _sut.AddAsync(new User { UserName = "alice", Email = "alice@example.com", PasswordHash = "h", CreatedOn = DateTime.UtcNow });
        await _fixture.Context.SaveChangesAsync();

        Assert.True(await _sut.AnyAsync());
    }
}
