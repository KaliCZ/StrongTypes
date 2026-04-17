#nullable enable

using System;
using System.Globalization;

namespace StrongTypes;

/// <summary>
/// Parsing extensions on <see cref="string"/>. Each method returns a nullable
/// result: <c>null</c> when the input is null/empty/whitespace or parsing
/// fails, a parsed value otherwise.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns a <see cref="NonEmptyString"/> wrapping <paramref name="s"/>, or
    /// <c>null</c> if <paramref name="s"/> is null, empty, or whitespace.
    /// </summary>
    public static NonEmptyString? AsNonEmpty(this string? s) => NonEmptyString.TryCreate(s);

    public static byte? AsByte(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        byte.TryParse(s, style, format, out var value) ? value : null;

    public static short? AsShort(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        short.TryParse(s, style, format, out var value) ? value : null;

    public static int? AsInt(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        int.TryParse(s, style, format, out var value) ? value : null;

    public static long? AsLong(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        long.TryParse(s, style, format, out var value) ? value : null;

    public static float? AsFloat(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        float.TryParse(s, style, format, out var value) ? value : null;

    public static double? AsDouble(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        double.TryParse(s, style, format, out var value) ? value : null;

    public static decimal? AsDecimal(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        decimal.TryParse(s, style, format, out var value) ? value : null;

    public static bool? AsBool(this string? s) =>
        bool.TryParse(s, out var value) ? value : null;

    public static DateTime? AsDateTime(this string? s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        DateTime.TryParse(s, format, style, out var value) ? value : null;

    public static TimeSpan? AsTimeSpan(this string? s, IFormatProvider? format = null) =>
        TimeSpan.TryParse(s, format, out var value) ? value : null;

    public static Guid? AsGuid(this string? s) =>
        Guid.TryParse(s, out var value) ? value : null;

    public static Guid? AsGuidExact(this string? s, string format = "D") =>
        Guid.TryParseExact(s, format, out var value) ? value : null;

    /// <summary>
    /// Parses a single named enum member of <typeparamref name="TEnum"/>. Returns
    /// <c>null</c> when the input is null/whitespace, contains a comma (so a
    /// caller cannot smuggle in a flag combination), is not a defined member,
    /// or differs from the parsed member's name in anything other than casing.
    /// </summary>
    public static TEnum? AsEnum<TEnum>(this string? s, bool ignoreCase = false)
        where TEnum : struct, Enum
    {
        if (s is null || s.Contains(',') || !Enum.TryParse<TEnum>(s, ignoreCase, out var value))
        {
            return null;
        }

        if (!Enum.IsDefined(value) || !value.ToString().Equals(s, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return value;
    }
}
