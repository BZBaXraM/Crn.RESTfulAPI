using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Tests.TestSupport;
using Xunit;

namespace Infrastructure.Tests.Data;

public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();
    private readonly UnitOfWork _sut;

    public UnitOfWorkTests()
    {
        _sut = new UnitOfWork(_fixture.Context);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void Repository_properties_return_the_same_instance_on_repeated_access()
    {
        Assert.Same(_sut.Products, _sut.Products);
        Assert.Same(_sut.Items, _sut.Items);
        Assert.Same(_sut.Users, _sut.Users);
        Assert.Same(_sut.RefreshTokens, _sut.RefreshTokens);
    }

    [Fact]
    public async Task SaveChangesAsync_persists_changes_made_through_any_repository()
    {
        await _sut.Products.AddAsync(new Product { ProductName = "Widget", CreatedBy = "tester", CreatedOn = DateTime.UtcNow });

        var affected = await _sut.SaveChangesAsync();

        Assert.Equal(1, affected);
    }
}
