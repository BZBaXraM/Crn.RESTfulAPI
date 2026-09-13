using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public sealed class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Item>> ListByProductAsync(int productId, int skip, int take, CancellationToken cancellationToken = default)
        => await Set.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountByProductAsync(int productId, CancellationToken cancellationToken = default)
        => Set.CountAsync(i => i.ProductId == productId, cancellationToken);
}
