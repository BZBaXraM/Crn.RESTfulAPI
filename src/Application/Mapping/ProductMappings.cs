using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.ProductName,
        product.CreatedBy,
        product.CreatedOn,
        product.ModifiedBy,
        product.ModifiedOn);
}
