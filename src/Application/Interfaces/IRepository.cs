using System.Linq.Expressions;

namespace Application.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
