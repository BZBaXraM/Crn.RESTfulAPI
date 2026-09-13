using Domain.Entities;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.TestSupport;
using Xunit;

namespace Infrastructure.Tests.Data;

public class ItemRepositoryTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();
    private readonly ProductRepository _products;
    private readonly ItemRepository _sut;

    public ItemRepositoryTests()
    {
        _products = new ProductRepository(_fixture.Context);
        _sut = new ItemRepository(_fixture.Context);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<int> SeedProductAsync()
    {
        var product = new Product { ProductName = "Widget", CreatedBy = "tester", CreatedOn = DateTime.UtcNow };
        await _products.AddAsync(product);
        await _fixture.Context.SaveChangesAsync();
        return product.Id;
    }

    [Fact]
    public async Task ListByProductAsync_only_returns_items_for_that_product()
    {
        var productId = await SeedProductAsync();
        var otherProductId = await SeedProductAsync();

        await _sut.AddAsync(new Item { ProductId = productId, Quantity = 1 });
        await _sut.AddAsync(new Item { ProductId = productId, Quantity = 2 });
        await _sut.AddAsync(new Item { ProductId = otherProductId, Quantity = 99 });
        await _fixture.Context.SaveChangesAsync();

        var items = await _sut.ListByProductAsync(productId, skip: 0, take: 10);

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(productId, i.ProductId));
    }

    [Fact]
    public async Task CountByProductAsync_counts_only_that_products_items()
    {
        var productId = await SeedProductAsync();
        await _sut.AddAsync(new Item { ProductId = productId, Quantity = 1 });
        await _sut.AddAsync(new Item { ProductId = productId, Quantity = 1 });
        await _fixture.Context.SaveChangesAsync();

        Assert.Equal(2, await _sut.CountByProductAsync(productId));
    }
}
