using Domain.Entities;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.TestSupport;
using Xunit;

namespace Infrastructure.Tests.Data;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();
    private readonly UserRepository _users;
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepositoryTests()
    {
        _users = new UserRepository(_fixture.Context);
        _sut = new RefreshTokenRepository(_fixture.Context);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<int> SeedUserAsync()
    {
        var user = new User { UserName = "alice", Email = "alice@example.com", PasswordHash = "h", CreatedOn = DateTime.UtcNow };
        await _users.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task GetByHashAsync_eagerly_loads_the_owning_user()
    {
        var userId = await SeedUserAsync();
        await _sut.AddAsync(new RefreshToken { UserId = userId, TokenHash = "hash-1", ExpiresOn = DateTime.UtcNow.AddDays(1), CreatedOn = DateTime.UtcNow });
        await _fixture.Context.SaveChangesAsync();

        var token = await _sut.GetByHashAsync("hash-1");

        Assert.NotNull(token);
        Assert.NotNull(token!.User);
        Assert.Equal("alice", token.User.UserName);
    }

    [Fact]
    public async Task ListActiveByUserAsync_excludes_revoked_and_expired_tokens()
    {
        var userId = await SeedUserAsync();
        var now = DateTime.UtcNow;

        await _sut.AddAsync(new RefreshToken { UserId = userId, TokenHash = "active", ExpiresOn = now.AddDays(1), CreatedOn = now });
        await _sut.AddAsync(new RefreshToken { UserId = userId, TokenHash = "expired", ExpiresOn = now.AddDays(-1), CreatedOn = now });
        await _sut.AddAsync(new RefreshToken { UserId = userId, TokenHash = "revoked", ExpiresOn = now.AddDays(1), CreatedOn = now, RevokedOn = now });
        await _fixture.Context.SaveChangesAsync();

        var active = await _sut.ListActiveByUserAsync(userId, now);

        Assert.Single(active);
        Assert.Equal("active", active[0].TokenHash);
    }
}
