using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
