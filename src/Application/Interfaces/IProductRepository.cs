using Domain.Entities;

namespace Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
