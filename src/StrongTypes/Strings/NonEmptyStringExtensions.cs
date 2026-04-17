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
}
