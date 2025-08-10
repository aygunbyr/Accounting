using FluentValidation;

namespace Accounting.Application.Invoices.Queries.List;

public class ListInvoicesValidator : AbstractValidator<ListInvoicesQuery>
{
    public ListInvoicesValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).Must(BeValidSort)
            .WithMessage("Sort must be field:dir where dir is asc|desc.");
    }

    private bool BeValidSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return true;
        var parts = sort.Split(':');
        if (parts.Length != 2) return false;
        var dir = parts[1].ToLowerInvariant();
        return dir is "asc" or "desc";
    }
}
