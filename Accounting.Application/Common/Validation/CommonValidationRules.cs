using Accounting.Application.Common.Utils;
using FluentValidation;

namespace Accounting.Application.Common.Validation;

/// <summary>
/// Tüm validator'larda kullanılabilecek ortak validation kuralları
/// </summary>
public static class CommonValidationRules
{
    /// <summary>
    /// String money amount validation (GreaterThan 0)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidMoneyAmount<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidMoneyAmount)
            .WithMessage("'{PropertyName}' must be a valid decimal number greater than 0.");
    }

    /// <summary>
    /// String money amount validation (GreaterThanOrEqualTo 0)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidMoneyAmountOrZero<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidMoneyAmountOrZero)
            .WithMessage("'{PropertyName}' must be a valid decimal number greater than or equal to 0.");
    }

    /// <summary>
    /// Quantity validation (1-3 decimal places, > 0)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidQuantity<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidQuantity)
            .WithMessage("'{PropertyName}' must be a valid quantity (1-3 decimal places, greater than 0).");
    }

    /// <summary>
    /// Unit Price validation (1-4 decimal places, >= 0)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidUnitPrice<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidUnitPrice)
            .WithMessage("'{PropertyName}' must be a valid unit price (1-4 decimal places, >= 0).");
    }

    /// <summary>
    /// Currency code validation (ISO-4217 whitelist)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidCurrency<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(3)
            .Must(BeValidCurrency)
            .WithMessage("'{PropertyName}' must be a valid currency code (TRY, USD, EUR, GBP).");
    }

    /// <summary>
    /// UTC DateTime string validation
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidUtcDateTime<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidUtcDateTime)
            .WithMessage("'{PropertyName}' must be a valid UTC datetime in ISO-8601 format.");
    }

    /// <summary>
    /// RowVersion Base64 validation
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidRowVersion<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeValidBase64)
            .WithMessage("'{PropertyName}' must be a valid Base64 string.");
    }

    // ========== Private Helper Methods ==========

    private static bool BeValidMoneyAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)) return false;
        if (!Money.TryParse2(amount, out var parsed)) return false;
        return parsed > 0m;
    }

    private static bool BeValidMoneyAmountOrZero(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)) return false;
        if (!Money.TryParse2(amount, out var parsed)) return false;
        return parsed >= 0m;
    }

    private static bool BeValidQuantity(string? qty)
    {
        if (string.IsNullOrWhiteSpace(qty)) return false;
        if (!Money.TryParse4(qty, out var parsed)) return false;
        if (parsed <= 0m) return false;

        var rounded = Money.R3(parsed);
        return true;
    }

    private static bool BeValidUnitPrice(string? unitPrice)
    {
        if (string.IsNullOrWhiteSpace(unitPrice)) return false;
        if (!Money.TryParse4(unitPrice, out var parsed)) return false;
        if (parsed < 0m) return false;

        var rounded = Money.R4(parsed);
        return true;
    }

    private static bool BeValidCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return false;

        var normalized = currency.ToUpperInvariant();
        var allowedCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };

        return allowedCurrencies.Contains(normalized);
    }

    private static bool BeValidUtcDateTime(string? dateUtc)
    {
        if (string.IsNullOrWhiteSpace(dateUtc)) return false;

        return DateTime.TryParse(dateUtc, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static bool BeValidBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return false;

        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}