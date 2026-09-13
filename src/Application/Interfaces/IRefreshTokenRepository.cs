using Domain.Entities;

namespace Application.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Loads the token together with its owning user.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);
}
