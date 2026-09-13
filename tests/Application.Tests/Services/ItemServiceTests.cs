using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

namespace Application.Tests.Services;

public class ItemServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IItemRepository> _items = new();
    private readonly ItemService _sut;

    public ItemServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _unitOfWork.SetupGet(u => u.Items).Returns(_items.Object);
        _sut = new ItemService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_throws_NotFoundException_when_product_missing()
    {
        _products.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(1, new CreateItemRequest(5)));

        _items.Verify(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_adds_item_scoped_to_product_when_product_exists()
    {
        _products.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Item? captured = null;
        _items.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
            .Callback<Item, CancellationToken>((i, _) => captured = i)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(1, new CreateItemRequest(9));

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.ProductId);
        Assert.Equal(9, captured.Quantity);
        Assert.Equal(9, result.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_item_belongs_to_different_product()
    {
        _items.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { Id = 3, ProductId = 2, Quantity = 1 });

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(productId: 1, itemId: 3));
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_item_does_not_exist()
    {
        _items.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Item?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1, 3));
    }

    [Fact]
    public async Task UpdateAsync_updates_quantity_for_owned_item()
    {
        var item = new Item { Id = 3, ProductId = 1, Quantity = 5 };
        _items.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _sut.UpdateAsync(1, 3, new UpdateItemRequest(20));

        Assert.Equal(20, result.Quantity);
        Assert.Equal(20, item.Quantity);
        _items.Verify(r => r.Update(item), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_removes_owned_item()
    {
        var item = new Item { Id = 3, ProductId = 1, Quantity = 5 };
        _items.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        await _sut.DeleteAsync(1, 3);

        _items.Verify(r => r.Remove(item), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByProductAsync_throws_when_product_missing_and_never_lists_items()
    {
        _products.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByProductAsync(1, new PaginationQuery()));

        _items.Verify(r => r.ListByProductAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByProductAsync_returns_paged_items_when_product_exists()
    {
        _products.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _items.Setup(r => r.ListByProductAsync(1, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Item> { new() { Id = 1, ProductId = 1, Quantity = 2 } });
        _items.Setup(r => r.CountByProductAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.GetByProductAsync(1, new PaginationQuery { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }
}
