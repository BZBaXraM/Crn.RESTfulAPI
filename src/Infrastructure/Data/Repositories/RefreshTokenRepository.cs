using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public sealed class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Set.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default)
        => await Set
            .Where(t => t.UserId == userId && t.RevokedOn == null && t.ExpiresOn > utcNow)
            .ToListAsync(cancellationToken);
}
