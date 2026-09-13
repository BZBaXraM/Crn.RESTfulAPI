namespace Application.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IItemRepository Items { get; }
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
