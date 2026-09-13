using Application.DTOs;
using Application.Validators;
using Xunit;

namespace Application.Tests.Validators;

public class CreateItemRequestValidatorTests
{
    private readonly CreateItemRequestValidator _sut = new();

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    public void Validates_quantity_is_non_negative(int quantity, bool expectedValid)
        => Assert.Equal(expectedValid, _sut.Validate(new CreateItemRequest(quantity)).IsValid);
}
