using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
