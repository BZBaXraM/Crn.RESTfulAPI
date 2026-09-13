using System.Linq.Expressions;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<TEntity> Set;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    public virtual Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Set.FindAsync([id], cancellationToken).AsTask();

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, CancellationToken cancellationToken = default)
        => predicate is null
            ? Set.CountAsync(cancellationToken)
            : Set.CountAsync(predicate, cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
