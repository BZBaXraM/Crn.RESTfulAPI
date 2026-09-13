using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public sealed class ItemService : IItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public ItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ItemDto>> GetByProductAsync(int productId, PaginationQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);

        var skip = (query.PageNumber - 1) * query.PageSize;
        var items = await _unitOfWork.Items.ListByProductAsync(productId, skip, query.PageSize, cancellationToken);
        var total = await _unitOfWork.Items.CountByProductAsync(productId, cancellationToken);

        return new PagedResult<ItemDto>(items.Select(i => i.ToDto()).ToList(), query.PageNumber, query.PageSize, total);
    }

    public async Task<ItemDto> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(productId, itemId, cancellationToken);
        return item.ToDto();
    }

    public async Task<ItemDto> CreateAsync(int productId, CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);

        var item = new Item { ProductId = productId, Quantity = request.Quantity };
        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }

    public async Task<ItemDto> UpdateAsync(int productId, int itemId, UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(productId, itemId, cancellationToken);

        item.Quantity = request.Quantity;
        _unitOfWork.Items.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }

    public async Task DeleteAsync(int productId, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(productId, itemId, cancellationToken);

        _unitOfWork.Items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Products.ExistsAsync(productId, cancellationToken))
        {
            throw new NotFoundException(nameof(Product), productId);
        }
    }

    private async Task<Item> GetOwnedItemAsync(int productId, int itemId, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(itemId, cancellationToken);
        if (item is null || item.ProductId != productId)
        {
            throw new NotFoundException(nameof(Item), itemId);
        }

        return item;
    }
}
