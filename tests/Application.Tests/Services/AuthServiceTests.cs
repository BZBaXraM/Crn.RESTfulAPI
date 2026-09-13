using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

namespace Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.RefreshTokens).Returns(_refreshTokens.Object);
        _sut = new AuthService(_unitOfWork.Object, _tokens.Object, _hasher.Object, _clock);
    }

    [Fact]
    public async Task RegisterAsync_throws_ConflictException_when_username_taken()
    {
        _users.Setup(r => r.UserNameExistsAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RegisterAsync(new RegisterRequest("alice", "alice@example.com", "Passw0rd!")));
    }

    [Fact]
    public async Task RegisterAsync_throws_ConflictException_when_email_taken()
    {
        _users.Setup(r => r.UserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.EmailExistsAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RegisterAsync(new RegisterRequest("alice", "alice@example.com", "Passw0rd!")));
    }

    [Fact]
    public async Task RegisterAsync_hashes_password_and_defaults_to_User_role()
    {
        _users.Setup(r => r.UserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Passw0rd!")).Returns("hashed-value");

        User? captured = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(new RegisterRequest("alice", "alice@example.com", "Passw0rd!"));

        Assert.Equal("hashed-value", captured!.PasswordHash);
        Assert.Equal(Role.User, captured.Role);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task LoginAsync_throws_AuthenticationFailedException_when_user_not_found()
    {
        _users.Setup(r => r.GetByUserNameAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.LoginAsync(new LoginRequest("ghost", "x")));
    }

    [Fact]
    public async Task LoginAsync_throws_AuthenticationFailedException_when_password_invalid()
    {
        var user = new User { Id = 1, UserName = "alice", PasswordHash = "hash" };
        _users.Setup(r => r.GetByUserNameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("hash", "wrong")).Returns(false);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.LoginAsync(new LoginRequest("alice", "wrong")));
    }

    [Fact]
    public async Task LoginAsync_issues_token_pair_on_valid_credentials()
    {
        var user = new User { Id = 1, UserName = "alice", PasswordHash = "hash", Role = Role.Admin };
        _users.Setup(r => r.GetByUserNameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("hash", "correct")).Returns(true);
        _tokens.Setup(t => t.CreateAccessToken(user)).Returns(new AccessToken("access-jwt", _clock.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.CreateRefreshToken()).Returns(new GeneratedRefreshToken("raw-refresh", "hashed-refresh", _clock.UtcNow.AddDays(7)));

        var result = await _sut.LoginAsync(new LoginRequest("alice", "correct"));

        Assert.Equal("access-jwt", result.AccessToken);
        Assert.Equal("raw-refresh", result.RefreshToken);
        _refreshTokens.Verify(r => r.AddAsync(
            It.Is<RefreshToken>(t => t.UserId == 1 && t.TokenHash == "hashed-refresh"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_throws_when_token_not_found()
    {
        _tokens.Setup(t => t.HashRefreshToken("unknown")).Returns("hash-of-unknown");
        _refreshTokens.Setup(r => r.GetByHashAsync("hash-of-unknown", It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.RefreshAsync(new RefreshTokenRequest("unknown")));
    }

    [Fact]
    public async Task RefreshAsync_throws_when_token_expired()
    {
        var user = new User { Id = 1, UserName = "alice" };
        var token = new RefreshToken { UserId = 1, User = user, TokenHash = "h", ExpiresOn = _clock.UtcNow.AddDays(-1) };
        _tokens.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("h");
        _refreshTokens.Setup(r => r.GetByHashAsync("h", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.RefreshAsync(new RefreshTokenRequest("raw")));
    }

    [Fact]
    public async Task RefreshAsync_rotates_token_and_revokes_the_presented_one()
    {
        var user = new User { Id = 1, UserName = "alice", Role = Role.User };
        var existing = new RefreshToken
        {
            Id = 10,
            UserId = 1,
            User = user,
            TokenHash = "old-hash",
            ExpiresOn = _clock.UtcNow.AddDays(1)
        };
        _tokens.Setup(t => t.HashRefreshToken("raw-old")).Returns("old-hash");
        _refreshTokens.Setup(r => r.GetByHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _tokens.Setup(t => t.CreateAccessToken(user)).Returns(new AccessToken("new-access", _clock.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.CreateRefreshToken()).Returns(new GeneratedRefreshToken("raw-new", "new-hash", _clock.UtcNow.AddDays(7)));

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("raw-old"));

        Assert.Equal("new-access", result.AccessToken);
        Assert.Equal("raw-new", result.RefreshToken);
        Assert.NotNull(existing.RevokedOn);
        Assert.Equal("new-hash", existing.ReplacedByTokenHash);
        _refreshTokens.Verify(r => r.Update(existing), Times.Once);
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "new-hash"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_reuse_of_already_revoked_token_kills_every_active_token_for_the_user()
    {
        var user = new User { Id = 1, UserName = "alice" };
        var stolen = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            User = user,
            TokenHash = "stolen-hash",
            ExpiresOn = _clock.UtcNow.AddDays(1),
            RevokedOn = _clock.UtcNow.AddMinutes(-5)
        };
        var otherActive = new RefreshToken { Id = 2, UserId = 1, TokenHash = "other-hash", ExpiresOn = _clock.UtcNow.AddDays(2) };

        _tokens.Setup(t => t.HashRefreshToken("stolen-raw")).Returns("stolen-hash");
        _refreshTokens.Setup(r => r.GetByHashAsync("stolen-hash", It.IsAny<CancellationToken>())).ReturnsAsync(stolen);
        _refreshTokens.Setup(r => r.ListActiveByUserAsync(1, _clock.UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { otherActive });

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.RefreshAsync(new RefreshTokenRequest("stolen-raw")));

        Assert.NotNull(otherActive.RevokedOn);
        _refreshTokens.Verify(r => r.Update(otherActive), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_marks_active_token_revoked()
    {
        var token = new RefreshToken { Id = 1, UserId = 1, TokenHash = "h", ExpiresOn = _clock.UtcNow.AddDays(1) };
        _tokens.Setup(t => t.HashRefreshToken("raw")).Returns("h");
        _refreshTokens.Setup(r => r.GetByHashAsync("h", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await _sut.RevokeAsync(new RefreshTokenRequest("raw"));

        Assert.NotNull(token.RevokedOn);
        _refreshTokens.Verify(r => r.Update(token), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_throws_when_token_already_inactive()
    {
        var token = new RefreshToken { Id = 1, UserId = 1, TokenHash = "h", ExpiresOn = _clock.UtcNow.AddDays(-1) };
        _tokens.Setup(t => t.HashRefreshToken("raw")).Returns("h");
        _refreshTokens.Setup(r => r.GetByHashAsync("h", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _sut.RevokeAsync(new RefreshTokenRequest("raw")));
    }
}
