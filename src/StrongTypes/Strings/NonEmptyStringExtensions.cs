using System;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace StrongTypes;

/// <summary>Parsing extensions on <see cref="NonEmptyString"/>. Each <c>As…</c> method returns <c>null</c> when parsing fails; each <c>To…</c> method throws.</summary>
public static class NonEmptyStringExtensions
{
    /// <summary>Returns the underlying <see cref="string"/> value.</summary>
    /// <remarks>StrongTypes.EfCore translates this call in LINQ expressions to the underlying string column, letting callers write <c>e.Value.Unwrap().Contains("foo")</c> against SQL.</remarks>
    [Pure]
    public static string Unwrap(this NonEmptyString s) => s.Value;

    [Pure]
    public static byte? AsByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsByte(format, style);

    [Pure]
    public static short? AsShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsShort(format, style);

    [Pure]
    public static int? AsInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsInt(format, style);

    [Pure]
    public static long? AsLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsLong(format, style);

    [Pure]
    public static float? AsFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.AsFloat(format, style);

    [Pure]
    public static double? AsDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.AsDouble(format, style);

    [Pure]
    public static decimal? AsDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        s.Value.AsDecimal(format, style);

    [Pure]
    public static bool? AsBool(this NonEmptyString s) =>
        s.Value.AsBool();

    [Pure]
    public static DateTime? AsDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        s.Value.AsDateTime(format, style);

    [Pure]
    public static TimeSpan? AsTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        s.Value.AsTimeSpan(format);

    [Pure]
    public static Guid? AsGuid(this NonEmptyString s) =>
        s.Value.AsGuid();

    [Pure]
    public static Guid? AsGuidExact(this NonEmptyString s, string format = "D") =>
        s.Value.AsGuidExact(format);

    [Pure]
    public static TEnum? AsEnum<TEnum>(this NonEmptyString s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        s.Value.AsEnum<TEnum>(ignoreCase);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="byte"/>.</exception>
    [Pure]
    public static byte ToByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToByte(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="short"/>.</exception>
    [Pure]
    public static short ToShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToShort(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="int"/>.</exception>
    [Pure]
    public static int ToInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToInt(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="long"/>.</exception>
    [Pure]
    public static long ToLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToLong(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    [Pure]
    public static float ToFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToFloat(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    [Pure]
    public static double ToDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToDouble(format, style);

    /// <exception cref="FormatException">The value is not in a valid numeric format.</exception>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="decimal"/>.</exception>
    [Pure]
    public static decimal ToDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        s.Value.ToDecimal(format, style);

    /// <exception cref="FormatException">The value is not <c>"True"</c> or <c>"False"</c>.</exception>
    [Pure]
    public static bool ToBool(this NonEmptyString s) => s.Value.ToBool();

    /// <exception cref="FormatException">The value is not a recognised date and time.</exception>
    [Pure]
    public static DateTime ToDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        s.Value.ToDateTime(format, style);

    /// <exception cref="FormatException">The value is not a valid time-span format.</exception>
    /// <exception cref="OverflowException">A parsed component of the value is out of range.</exception>
    [Pure]
    public static TimeSpan ToTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        s.Value.ToTimeSpan(format);

    /// <exception cref="FormatException">The value is not in a recognised GUID format.</exception>
    [Pure]
    public static Guid ToGuid(this NonEmptyString s) => s.Value.ToGuid();

    /// <param name="s">The wrapper to parse.</param>
    /// <param name="format">The exact format specifier the value must match (e.g. <c>"D"</c>, <c>"N"</c>, <c>"B"</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value does not match <paramref name="format"/>.</exception>
    [Pure]
    public static Guid ToGuidExact(this NonEmptyString s, string format = "D") =>
        s.Value.ToGuidExact(format);

    /// <exception cref="ArgumentException">The value is not the name or value of a member of <typeparamref name="TEnum"/>.</exception>
    [Pure]
    public static TEnum ToEnum<TEnum>(this NonEmptyString s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        s.Value.ToEnum<TEnum>(ignoreCase);
}
