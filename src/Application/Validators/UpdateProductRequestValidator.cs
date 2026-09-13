using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty()
            .MaximumLength(255);
    }
}
