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

    private static bool TryParseWithScale(string? input, int scale, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // normalize spaces & NBSP
        var s = input.Trim().Replace("\u00A0", "").Replace(" ", "");

        // 1) Düz denemeler: invariant ve TR
        if (decimal.TryParse(s, NumberStyles.Number, Inv, out var v) ||
            decimal.TryParse(s, NumberStyles.Number, Tr, out v))
        {
            value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
            return true;
        }

        // 2) Manuel normalize: binlikleri at, virgülü noktaya çevir (TR girişleri için)
        // Örn: "1.234,56" -> "1234.56"
        var normalized = s.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(normalized, NumberStyles.Number, Inv, out v))
        {
            value = Math.Round(v, scale, MidpointRounding.AwayFromZero);
            return true;
        }

        return false;
    }
}
