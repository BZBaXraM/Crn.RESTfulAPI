using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<Product>> ListAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>>? predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = Set.AsNoTracking().OrderBy(p => p.Id);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => Set.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);
}
