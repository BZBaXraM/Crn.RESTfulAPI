using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

public interface IItemService
{
    Task<PagedResult<ItemDto>> GetByProductAsync(int productId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ItemDto> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken = default);
    Task<ItemDto> CreateAsync(int productId, CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<ItemDto> UpdateAsync(int productId, int itemId, UpdateItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, int itemId, CancellationToken cancellationToken = default);
}
