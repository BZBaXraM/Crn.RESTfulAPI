using Domain.Entities;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.TestSupport;
using Xunit;

namespace Infrastructure.Tests.Data;

public class ProductRepositoryTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();
    private readonly ProductRepository _sut;

    public ProductRepositoryTests()
    {
        _sut = new ProductRepository(_fixture.Context);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_then_SaveChanges_persists_the_product()
    {
        var product = new Product { ProductName = "Widget", CreatedBy = "tester", CreatedOn = DateTime.UtcNow };

        await _sut.AddAsync(product);
        await _fixture.Context.SaveChangesAsync();

        Assert.True(product.Id > 0);
        var reloaded = await _sut.GetByIdAsync(product.Id);
        Assert.Equal("Widget", reloaded!.ProductName);
    }

    [Fact]
    public async Task ExistsAsync_returns_false_for_unknown_id()
        => Assert.False(await _sut.ExistsAsync(999));

    [Fact]
    public async Task ExistsAsync_returns_true_after_insert()
    {
        var product = new Product { ProductName = "Widget", CreatedBy = "tester", CreatedOn = DateTime.UtcNow };
        await _sut.AddAsync(product);
        await _fixture.Context.SaveChangesAsync();

        Assert.True(await _sut.ExistsAsync(product.Id));
    }

    [Fact]
    public async Task ListAsync_paginates_in_id_order()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _sut.AddAsync(new Product { ProductName = $"P{i}", CreatedBy = "tester", CreatedOn = DateTime.UtcNow });
        }
        await _fixture.Context.SaveChangesAsync();

        var page = await _sut.ListAsync(null, skip: 2, take: 2);

        Assert.Equal(2, page.Count);
        Assert.Equal("P3", page[0].ProductName);
        Assert.Equal("P4", page[1].ProductName);
    }

    [Fact]
    public async Task Remove_deletes_the_product()
    {
        var product = new Product { ProductName = "ToDelete", CreatedBy = "tester", CreatedOn = DateTime.UtcNow };
        await _sut.AddAsync(product);
        await _fixture.Context.SaveChangesAsync();

        _sut.Remove(product);
        await _fixture.Context.SaveChangesAsync();

        Assert.Null(await _sut.GetByIdAsync(product.Id));
    }
}
