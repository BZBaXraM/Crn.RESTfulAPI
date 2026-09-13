using Domain.Entities;

namespace Application.Interfaces;

public interface IItemRepository : IRepository<Item>
{
    Task<IReadOnlyList<Item>> ListByProductAsync(int productId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountByProductAsync(int productId, CancellationToken cancellationToken = default);
}
