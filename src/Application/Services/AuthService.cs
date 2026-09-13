using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokens, IPasswordHasher hasher, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _tokens = tokens;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.UserNameExistsAsync(request.UserName, cancellationToken))
        {
            throw new ConflictException($"User name '{request.UserName}' is already taken.");
        }

        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException($"Email '{request.Email}' is already registered.");
        }

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = Role.User,
            CreatedOn = _clock.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByUserNameAsync(request.UserName, cancellationToken);
        if (user is null || !_hasher.Verify(user.PasswordHash, request.Password))
        {
            throw new AuthenticationFailedException();
        }

        var response = await IssueTokensAsync(user, replacing: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var existing = await _unitOfWork.RefreshTokens.GetByHashAsync(_tokens.HashRefreshToken(request.RefreshToken), cancellationToken)
                       ?? throw new AuthenticationFailedException("Invalid refresh token.");

        if (existing.IsRevoked)
        {
            // Reuse of an already-rotated token means it was likely stolen: kill the whole family.
            await RevokeAllActiveAsync(existing.UserId, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException("Refresh token has been revoked.");
        }

        if (existing.IsExpired(now))
        {
            throw new AuthenticationFailedException("Refresh token has expired.");
        }

        var response = await IssueTokensAsync(existing.User, replacing: existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var existing = await _unitOfWork.RefreshTokens.GetByHashAsync(_tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (existing is null || !existing.IsActive(now))
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        existing.RevokedOn = now;
        _unitOfWork.RefreshTokens.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, RefreshToken? replacing, CancellationToken cancellationToken)
    {
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken();
        var now = _clock.UtcNow;

        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresOn = refresh.ExpiresOn,
            CreatedOn = now
        };

        if (replacing is not null)
        {
            replacing.RevokedOn = now;
            replacing.ReplacedByTokenHash = refresh.TokenHash;
            _unitOfWork.RefreshTokens.Update(replacing);
        }

        await _unitOfWork.RefreshTokens.AddAsync(entity, cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresOn, refresh.RawToken, refresh.ExpiresOn);
    }

    private async Task RevokeAllActiveAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var active = await _unitOfWork.RefreshTokens.ListActiveByUserAsync(userId, now, cancellationToken);
        foreach (var token in active)
        {
            token.RevokedOn = now;
            _unitOfWork.RefreshTokens.Update(token);
        }
    }
}
