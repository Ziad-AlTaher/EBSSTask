using EBSSTask.Application.DTOs.Balance;
using FluentValidation;

namespace EBSSTask.Application.Validators;

public class BalanceQueryParametersValidator : AbstractValidator<BalanceQueryParameters>
{
    public BalanceQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(100)
            .WithMessage("Search query must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Search));
    }
}
