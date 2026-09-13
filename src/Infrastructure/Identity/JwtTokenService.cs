using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenBytes = 64;

    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var identity = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        ]);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new GeneratedRefreshToken(raw, HashRefreshToken(raw), _clock.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    public string HashRefreshToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
