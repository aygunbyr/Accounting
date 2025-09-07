using System.Globalization;

namespace Accounting.Application.Common.Utils;

/// <summary>
/// Finansal yuvarlama/formatlama yardımcıları.
/// Politika: MidpointRounding.AwayFromZero (5'ler yukarı)
/// </summary>
public static class Money
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // Yuvarlama (decimal -> decimal)
    public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    public static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
    public static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    // Format (decimal -> "F2"/"F4" string)
    public static string S2(decimal v) => R2(v).ToString("F2", Inv);
    public static string S3(decimal v) => R3(v).ToString("F3", Inv);
    public static string S4(decimal v) => R4(v).ToString("F4", Inv);
}
