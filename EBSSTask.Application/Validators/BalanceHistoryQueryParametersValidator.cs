using EBSSTask.Application.DTOs.BalanceHistory;
using FluentValidation;

namespace EBSSTask.Application.Validators;

public class BalanceHistoryQueryParametersValidator : AbstractValidator<BalanceHistoryQueryParameters>
{
    public BalanceHistoryQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.BalanceId)
            .GreaterThan(0)
            .WithMessage("BalanceId must be greater than 0.")
            .When(x => x.BalanceId.HasValue);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .WithMessage("ToDate must be greater than or equal to FromDate.")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
