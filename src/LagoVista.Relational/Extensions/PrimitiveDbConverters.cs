using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DateParsingExtensions
{
    private static readonly string[] SupportedFormats = new[]
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd"
    };

    public static DateOnly ParseToDateOnly(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Date value cannot be null or empty.", nameof(input));

        if (DateTime.TryParseExact(
                input.Trim(),
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }

        throw new FormatException(
            $"Invalid date format '{input}'. Supported formats: yyyy-MM-dd, yyyy/MM/dd, yyyyMMdd.");
    }

    public static bool TryParseToDateOnly(this string input, out DateOnly result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (DateTime.TryParseExact(
                input.Trim(),
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            result = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }
}