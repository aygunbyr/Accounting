using System.Globalization;

namespace Accounting.Application.Common.Utils;

/// <summary>
/// Finansal yuvarlama, formatlama ve parse yardımcıları.
/// 
/// Politika:
/// - Yuvarlama: MidpointRounding.AwayFromZero (5'ler yukarı)
/// - Format: InvariantCulture (ondalık ayracı: nokta)
/// 
/// Örnek: "1234.56" ✓  |  "1234,56" ✗
/// 
/// Not: Kullanıcı arayüzünde locale dönüşümü frontend'in sorumluluğundadır.
/// </summary>
public static class Money
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    #region Round (decimal -> decimal)

    public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    public static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
    public static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    #endregion

    #region Format (decimal -> string)

    public static string S2(decimal v) => R2(v).ToString("F2", Inv);
    public static string S3(decimal v) => R3(v).ToString("F3", Inv);
    public static string S4(decimal v) => R4(v).ToString("F4", Inv);

    #endregion

    #region Parse (string -> decimal)

    public static bool TryParse2(string? input, out decimal value) => TryParse(input, 2, out value);
    public static bool TryParse3(string? input, out decimal value) => TryParse(input, 3, out value);
    public static bool TryParse4(string? input, out decimal value) => TryParse(input, 4, out value);

    public static decimal Parse2(string input) => Parse(input, 2);
    public static decimal Parse3(string input) => Parse(input, 3);
    public static decimal Parse4(string input) => Parse(input, 4);

    #endregion

    #region Private

    private static bool TryParse(string? input, int decimals, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (!decimal.TryParse(input.Trim(), NumberStyles.Number, Inv, out var parsed))
            return false;

        value = Math.Round(parsed, decimals, MidpointRounding.AwayFromZero);
        return true;
    }

    private static decimal Parse(string input, int decimals)
    {
        if (!TryParse(input, decimals, out var value))
            throw new FormatException($"Invalid decimal format: '{input}'. Expected format: 1234.56");

        return value;
    }

    #endregion
}
