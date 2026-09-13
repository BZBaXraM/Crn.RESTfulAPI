using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}
