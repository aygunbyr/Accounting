﻿using FluentValidation;

namespace Accounting.Application.Expenses.Queries.List;

public class ListExpensesValidator : AbstractValidator<ListExpensesQuery>
{
    public ListExpensesValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.Sort)
            .Must(BeValidSort)
            .WithMessage("Sort must be 'dateUtc|amount:asc|desc'.");

        RuleFor(x => x.Currency)
            .Must(c => string.IsNullOrWhiteSpace(c) || c!.Length == 3)
            .WithMessage("Currency must be 3 letters.");

        RuleFor(x => x.DateFromUtc).Must(BeIso8601OrNull)
            .WithMessage("DateFromUtc must be ISO-8601 (e.g. 2025-08-08T10:00:00Z).");

        RuleFor(x => x.DateToUtc).Must(BeIso8601OrNull)
            .WithMessage("DateToUtc must be ISO-8601 (e.g. 2025-08-08T10:00:00Z).");


    }
    private static bool BeValidSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return true;
        var parts = sort.Split(':');
        if (parts.Length != 2) return false;
        var field = parts[0].ToLowerInvariant();
        var dir = parts[1].ToLowerInvariant();
        return (field is "dateutc" or "amount") && (dir is "asc" or "desc");
    }

    private static bool BeIso8601OrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal, out _);
    }
}
