using Domain.Entities;

namespace Application.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresOn);

/// <summary>Raw refresh token (returned to the client) and the hash that gets persisted.</summary>
public sealed record GeneratedRefreshToken(string RawToken, string TokenHash, DateTime ExpiresOn);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    GeneratedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string rawToken);
}
