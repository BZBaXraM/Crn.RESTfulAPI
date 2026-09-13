using System.Linq.Expressions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

namespace Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUserService _currentUser = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _sut = new ProductService(_unitOfWork.Object, _currentUser, _clock);
    }

    [Fact]
    public async Task CreateAsync_persists_product_stamped_with_current_user_and_time()
    {
        _currentUser.UserName = "alice";
        _clock.UtcNow = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        Product? captured = null;
        _products.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateProductRequest("Widget"));

        Assert.NotNull(captured);
        Assert.Equal("Widget", captured!.ProductName);
        Assert.Equal("alice", captured.CreatedBy);
        Assert.Equal(_clock.UtcNow, captured.CreatedOn);
        Assert.Equal("Widget", result.ProductName);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_falls_back_to_system_when_no_current_user()
    {
        _currentUser.UserName = null;

        Product? captured = null;
        _products.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateProductRequest("Widget"));

        Assert.Equal("system", captured!.CreatedBy);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _products.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(42));
    }

    [Fact]
    public async Task GetByIdAsync_returns_mapped_dto_when_found()
    {
        var product = new Product { Id = 5, ProductName = "Gadget", CreatedBy = "admin", CreatedOn = _clock.UtcNow };
        _products.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(5);

        Assert.Equal(5, result.Id);
        Assert.Equal("Gadget", result.ProductName);
    }

    [Fact]
    public async Task UpdateAsync_stamps_modified_by_and_on()
    {
        var product = new Product { Id = 1, ProductName = "Old", CreatedBy = "admin", CreatedOn = _clock.UtcNow };
        _products.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _currentUser.UserName = "bob";
        _clock.UtcNow = _clock.UtcNow.AddDays(1);

        var result = await _sut.UpdateAsync(1, new UpdateProductRequest("New"));

        Assert.Equal("New", result.ProductName);
        Assert.Equal("bob", product.ModifiedBy);
        Assert.Equal(_clock.UtcNow, product.ModifiedOn);
        _products.Verify(r => r.Update(product), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(99, new UpdateProductRequest("X")));
    }

    [Fact]
    public async Task DeleteAsync_removes_existing_product()
    {
        var product = new Product { Id = 7 };
        _products.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.DeleteAsync(7);

        _products.Verify(r => r.Remove(product), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task GetPagedAsync_computes_skip_from_page_number_and_size()
    {
        var query = new PaginationQuery { PageNumber = 3, PageSize = 10 };
        _products.Setup(r => r.ListAsync(null, 20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { new() { Id = 1, ProductName = "A" } });
        _products.Setup(r => r.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(25);

        var result = await _sut.GetPagedAsync(query);

        Assert.Single(result.Items);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        _products.Verify(r => r.ListAsync(null, 20, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
