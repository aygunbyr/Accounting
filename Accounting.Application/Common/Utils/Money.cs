using System.Globalization;

namespace Accounting.Application.Common.Utils;

/// <summary>
/// Finansal yuvarlama/formatlama & parse yardımcıları.
/// Politika: MidpointRounding.AwayFromZero (5'ler yukarı)
/// </summary>
public static class Money
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

    // ---------- Round (decimal -> decimal)
    public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    public static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
    public static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    // ---------- String format (decimal -> "F*" string)
    public static string S2(decimal v) => R2(v).ToString("F2", Inv);
    public static string S3(decimal v) => R3(v).ToString("F3", Inv);
    public static string S4(decimal v) => R4(v).ToString("F4", Inv);

    // ---------- Parse helpers (string -> decimal, AwayFromZero)
    public static bool TryParse2(string? input, out decimal value) =>
        TryParseWithScale(input, 2, out value);

    public static bool TryParse4(string? input, out decimal value) =>
        TryParseWithScale(input, 4, out value);

    public static bool TryParse3(string? input, out decimal value) =>
        TryParseWithScale(input, 3, out value);

    public static decimal Parse2(string input)
    {
        if (!TryParse2(input, out var v)) throw new FormatException("Invalid money(2) format.");
        return v;
    }

    public static decimal Parse4(string input)
    {
        if (!TryParse4(input, out var v)) throw new FormatException("Invalid money(4) format.");
        return v;
    }

    public static decimal Parse3(string input)
    {
        if (!TryParse3(input, out var v)) throw new FormatException("Invalid money(3) format.");
        return v;
    }

    private static bool TryParseWithScale(string? input, int scale, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // normalize spaces & NBSP
        var s = input.Trim().Replace("\u00A0", "").Replace(" ", "");

        // Önce formatı tespit et:
        // - Hem nokta hem virgül varsa: format tespiti yap
        // - Sadece nokta varsa: belirsiz (1.234 = 1234 mü, 1.234 mü?)
        // - Sadece virgül varsa: TR format (virgül = ondalık)

        var hasDot = s.Contains('.');
        var hasComma = s.Contains(',');

        // 1) Hem nokta hem virgül var: "1.234,56" (TR) veya "1,234.56" (US)
        if (hasDot && hasComma)
        {
            var lastDot = s.LastIndexOf('.');
            var lastComma = s.LastIndexOf(',');

            if (lastComma > lastDot)
            {
                // TR format: "1.234,56" -> binlik=nokta, ondalık=virgül
                var normalized = s.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(normalized, NumberStyles.Number, Inv, out var v))
                {
                    value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
                    return true;
                }
            }
            else
            {
                // US format: "1,234.56" -> binlik=virgül, ondalık=nokta
                var normalized = s.Replace(",", "");
                if (decimal.TryParse(normalized, NumberStyles.Number, Inv, out var v))
                {
                    value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
                    return true;
                }
            }
        }
        // 2) Sadece virgül var: TR format "1234,56"
        else if (hasComma && !hasDot)
        {
            var normalized = s.Replace(",", ".");
            if (decimal.TryParse(normalized, NumberStyles.Number, Inv, out var v))
            {
                value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
                return true;
            }
        }
        // 3) Sadece nokta var veya hiç ayırıcı yok
        else
        {
            // Nokta pozisyonuna göre karar ver:
            // - "1.234" -> 3 hane sonra = muhtemelen binlik (TR) = 1234
            // - "1.23" -> 2 hane sonra = muhtemelen ondalık = 1.23
            // - "1.2" -> 1 hane sonra = muhtemelen ondalık = 1.2
            if (hasDot)
            {
                var dotIndex = s.LastIndexOf('.');
                var digitsAfterDot = s.Length - dotIndex - 1;

                // 3 veya daha fazla hane: binlik ayırıcı olarak kabul et
                if (digitsAfterDot >= 3)
                {
                    var normalized = s.Replace(".", "");
                    if (decimal.TryParse(normalized, NumberStyles.Number, Inv, out var v))
                    {
                        value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
                        return true;
                    }
                }
            }

            // Standart invariant parse
            if (decimal.TryParse(s, NumberStyles.Number, Inv, out var val))
            {
                value = Math.Round(val, scale, MidpointRounding.AwayFromZero);
                return true;
            }
        }

        return false;
    }
}
