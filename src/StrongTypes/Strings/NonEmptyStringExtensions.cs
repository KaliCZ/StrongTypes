#nullable enable

using System;
using System.Globalization;

namespace StrongTypes;

/// <summary>Parsing extensions on <see cref="NonEmptyString"/>. Each <c>As…</c> method returns <c>null</c> when parsing fails; each <c>To…</c> method throws.</summary>
public static class NonEmptyStringExtensions
{
    /// <summary>Returns the underlying <see cref="string"/> value.</summary>
    /// <param name="s">The wrapper.</param>
    /// <remarks>StrongTypes.EfCore translates this call in LINQ expressions to the underlying string column, letting callers write <c>e.Value.Unwrap().Contains("foo")</c> against SQL.</remarks>
    public static string Unwrap(this NonEmptyString s) => s.Value;

    public static byte? AsByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsByte(format, style);

    public static short? AsShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsShort(format, style);

    public static int? AsInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsInt(format, style);

    public static long? AsLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.AsLong(format, style);

    public static float? AsFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.AsFloat(format, style);

    public static double? AsDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.AsDouble(format, style);

    public static decimal? AsDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        s.Value.AsDecimal(format, style);

    public static bool? AsBool(this NonEmptyString s) =>
        s.Value.AsBool();

    public static DateTime? AsDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        s.Value.AsDateTime(format, style);

    public static TimeSpan? AsTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        s.Value.AsTimeSpan(format);

    public static Guid? AsGuid(this NonEmptyString s) =>
        s.Value.AsGuid();

    public static Guid? AsGuidExact(this NonEmptyString s, string format = "D") =>
        s.Value.AsGuidExact(format);

    public static TEnum? AsEnum<TEnum>(this NonEmptyString s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        s.Value.AsEnum<TEnum>(ignoreCase);

    // --- Throwing variants. Delegate to the string overloads, which in ---
    // --- turn call the framework Parse methods and throw on failure.   ---

    public static byte ToByte(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToByte(format, style);

    public static short ToShort(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToShort(format, style);

    public static int ToInt(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToInt(format, style);

    public static long ToLong(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        s.Value.ToLong(format, style);

    public static float ToFloat(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToFloat(format, style);

    public static double ToDouble(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        s.Value.ToDouble(format, style);

    public static decimal ToDecimal(this NonEmptyString s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        s.Value.ToDecimal(format, style);

    public static bool ToBool(this NonEmptyString s) => s.Value.ToBool();

    public static DateTime ToDateTime(this NonEmptyString s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        s.Value.ToDateTime(format, style);

    public static TimeSpan ToTimeSpan(this NonEmptyString s, IFormatProvider? format = null) =>
        s.Value.ToTimeSpan(format);

    public static Guid ToGuid(this NonEmptyString s) => s.Value.ToGuid();

    public static Guid ToGuidExact(this NonEmptyString s, string format = "D") =>
        s.Value.ToGuidExact(format);

    public static TEnum ToEnum<TEnum>(this NonEmptyString s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        s.Value.ToEnum<TEnum>(ignoreCase);
}
