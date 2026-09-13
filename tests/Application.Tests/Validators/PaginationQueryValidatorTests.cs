using Application.Common;
using Application.Validators;
using Xunit;

namespace Application.Tests.Validators;

public class PaginationQueryValidatorTests
{
    private readonly PaginationQueryValidator _sut = new();

    [Fact]
    public void Fails_when_page_number_is_zero()
        => Assert.False(_sut.Validate(new PaginationQuery { PageNumber = 0 }).IsValid);

    [Fact]
    public void Passes_for_defaults()
        => Assert.True(_sut.Validate(new PaginationQuery()).IsValid);

    [Fact]
    public void PageSize_is_clamped_to_max_before_validation_ever_sees_it()
    {
        var query = new PaginationQuery { PageSize = 5000 };

        Assert.Equal(PaginationQuery.MaxPageSize, query.PageSize);
        Assert.True(_sut.Validate(query).IsValid);
    }
}
