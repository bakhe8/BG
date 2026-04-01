using System.Globalization;

namespace BG.Application.Common;

public static class StructuredInputParser
{
    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static bool TryParseDate(string? value, out DateOnly date)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            date = default;
            return false;
        }

        return DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateOnly.TryParse(normalized, out date);
    }

    public static bool TryParseAmount(string? value, out decimal amount)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            amount = default;
            return false;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }
}
