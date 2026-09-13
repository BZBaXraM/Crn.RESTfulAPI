using Application.Common;
using FluentValidation;

namespace Application.Validators;

public sealed class PaginationQueryValidator : AbstractValidator<PaginationQuery>
{
    public PaginationQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationQuery.MaxPageSize);
    }
}
