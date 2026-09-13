namespace Application.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresOn,
    string RefreshToken,
    DateTime RefreshTokenExpiresOn);
