using Application.DTOs;
using Application.Validators;
using Xunit;

namespace Application.Tests.Validators;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _sut = new();

    [Fact]
    public void Fails_when_name_empty()
        => Assert.False(_sut.Validate(new CreateProductRequest("")).IsValid);

    [Fact]
    public void Fails_when_name_exceeds_max_length()
        => Assert.False(_sut.Validate(new CreateProductRequest(new string('a', 256))).IsValid);

    [Fact]
    public void Passes_for_valid_name()
        => Assert.True(_sut.Validate(new CreateProductRequest("Widget")).IsValid);
}
