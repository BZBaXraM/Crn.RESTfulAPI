using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ItemMappings
{
    public static ItemDto ToDto(this Item item) => new(item.Id, item.ProductId, item.Quantity);
}
