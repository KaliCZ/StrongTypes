using System;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace StrongTypes;

/// <summary>
/// Parsing extensions on <see cref="NonEmptyString"/>. Each method returns a
/// nullable result: <c>null</c> when parsing fails, a parsed value otherwise.
/// </summary>
public static class NonEmptyStringExtensions
{
    /// <summary>
    /// Returns the underlying <see cref="string"/> value. Intended for LINQ
    /// expressions translated by EF Core: the StrongTypes.EfCore package
    /// registers a method call translator that rewrites this call as the
    /// underlying string column, so callers can write
    /// <c>e.Value.Unwrap().Contains("foo")</c> and have EF translate it to
    /// SQL against the underlying string column.
    /// </summary>
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

    // --- Throwing variants. Delegate to the string overloads, which in ---
    // --- turn call the framework Parse methods and throw on failure.   ---

    [Pure]
    public static byte ToByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToByte(format, style);

    [Pure]
    public static short ToShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToShort(format, style);

    [Pure]
    public static int ToInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToInt(format, style);

    [Pure]
    public static long ToLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToLong(format, style);

    [Pure]
    public static float ToFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToFloat(format, style);

    [Pure]
    public static double ToDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToDouble(format, style);

    [Pure]
    public static decimal ToDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        s.Value.ToDecimal(format, style);

    [Pure]
    public static bool ToBool(this NonEmptyString s) => s.Value.ToBool();

    [Pure]
    public static DateTime ToDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        s.Value.ToDateTime(format, style);

    [Pure]
    public static TimeSpan ToTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        s.Value.ToTimeSpan(format);

    [Pure]
    public static Guid ToGuid(this NonEmptyString s) => s.Value.ToGuid();

    [Pure]
    public static Guid ToGuidExact(this NonEmptyString s, string format = "D") =>
        s.Value.ToGuidExact(format);

    [Pure]
    public static TEnum ToEnum<TEnum>(this NonEmptyString s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        s.Value.ToEnum<TEnum>(ignoreCase);
}
