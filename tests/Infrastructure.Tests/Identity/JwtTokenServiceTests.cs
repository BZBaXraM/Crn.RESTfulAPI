using Infrastructure.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Infrastructure.Tests.Identity;

public class JwtTokenServiceTests
{
    private readonly FakeDateTimeProvider _clock = new();
    private readonly JwtSettings _settings = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = new string('k', 32),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private JwtTokenService CreateSut() => new(Options.Create(_settings), _clock);

    [Fact]
    public void CreateAccessToken_embeds_id_name_email_and_role_claims()
    {
        var sut = CreateSut();
        var user = new User { Id = 42, UserName = "alice", Email = "alice@example.com", Role = Role.Admin };

        var token = sut.CreateAccessToken(user);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "test-audience");
        Assert.Contains(jwt.Claims, c => c.Type == "sub" && c.Value == "42");
        Assert.Contains(jwt.Claims, c => c.Value == "alice");
        Assert.Contains(jwt.Claims, c => c.Value == "Admin");
        Assert.Equal(_clock.UtcNow.AddMinutes(15), token.ExpiresOn);
    }

    [Fact]
    public async Task CreateAccessToken_produces_a_token_that_validates_against_the_same_signing_key()
    {
        var sut = CreateSut();
        var token = sut.CreateAccessToken(new User { Id = 1, UserName = "alice", Email = "a@b.com", Role = Role.User });

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.SigningKey)),
            // The token was minted against the fake clock's fixed instant, not the real system clock,
            // so lifetime checking here would fail whenever that fixed instant lies in the past. This
            // test only cares that the signature, issuer, and audience validate correctly.
            ValidateLifetime = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateRefreshToken_returns_a_raw_value_whose_hash_matches_HashRefreshToken()
    {
        var sut = CreateSut();

        var refresh = sut.CreateRefreshToken();

        Assert.Equal(refresh.TokenHash, sut.HashRefreshToken(refresh.RawToken));
        Assert.Equal(_clock.UtcNow.AddDays(7), refresh.ExpiresOn);
    }

    [Fact]
    public void CreateRefreshToken_generates_unique_values_each_call()
    {
        var sut = CreateSut();

        var first = sut.CreateRefreshToken();
        var second = sut.CreateRefreshToken();

        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.NotEqual(first.TokenHash, second.TokenHash);
    }

    [Fact]
    public void HashRefreshToken_is_deterministic()
    {
        var sut = CreateSut();

        Assert.Equal(sut.HashRefreshToken("same-input"), sut.HashRefreshToken("same-input"));
    }
}
