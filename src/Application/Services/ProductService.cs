using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ProductService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var skip = (query.PageNumber - 1) * query.PageSize;
        var items = await _unitOfWork.Products.ListAsync(null, skip, query.PageSize, cancellationToken);
        var total = await _unitOfWork.Products.CountAsync(null, cancellationToken);

        return new PagedResult<ProductDto>(items.Select(p => p.ToDto()).ToList(), query.PageNumber, query.PageSize, total);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Product), id);
        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            ProductName = request.ProductName,
            CreatedBy = _currentUser.UserName ?? "system",
            CreatedOn = _clock.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Product), id);

        product.ProductName = request.ProductName;
        product.ModifiedBy = _currentUser.UserName ?? "system";
        product.ModifiedOn = _clock.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Product), id);

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
