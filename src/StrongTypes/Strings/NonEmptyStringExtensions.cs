#nullable enable

using System;
using System.Globalization;

namespace StrongTypes;

/// <summary>
/// Parsing extensions on <see cref="NonEmptyString"/>. Each method returns a
/// nullable result: <c>null</c> when parsing fails, a parsed value otherwise.
/// </summary>
public static class NonEmptyStringExtensions
{
    public static byte? ToByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        byte.TryParse(s.Value, style, format, out var value) ? value : null;

    public static short? ToShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        short.TryParse(s.Value, style, format, out var value) ? value : null;

    public static int? ToInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        int.TryParse(s.Value, style, format, out var value) ? value : null;

    public static long? ToLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        long.TryParse(s.Value, style, format, out var value) ? value : null;

    public static float? ToFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        float.TryParse(s.Value, style, format, out var value) ? value : null;

    public static double? ToDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        double.TryParse(s.Value, style, format, out var value) ? value : null;

    public static decimal? ToDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        decimal.TryParse(s.Value, style, format, out var value) ? value : null;

    public static bool? ToBool(this NonEmptyString s) =>
        bool.TryParse(s.Value, out var value) ? value : null;

    public static DateTime? ToDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        DateTime.TryParse(s.Value, format, style, out var value) ? value : null;

    public static TimeSpan? ToTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        TimeSpan.TryParse(s.Value, format, out var value) ? value : null;

    public static Guid? ToGuid(this NonEmptyString s) =>
        Guid.TryParse(s.Value, out var value) ? value : null;

    public static Guid? ToGuidExact(this NonEmptyString s, string format = "D") =>
        Guid.TryParseExact(s.Value, format, out var value) ? value : null;
}
