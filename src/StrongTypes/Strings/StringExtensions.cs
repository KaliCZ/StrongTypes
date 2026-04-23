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
    /// Parses <paramref name="s"/> as a member of <typeparamref name="TEnum"/>
    /// via <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>. Returns
    /// <c>null</c> when parsing fails. Equivalent to <c>TEnum.TryParse(s, ignoreCase)</c>;
    /// the indirection exists because C# does not allow calling extension-static
    /// members through an open type parameter.
    /// </summary>
    public static TEnum? AsEnum<TEnum>(this string? s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(s, ignoreCase, out var v) ? v : null;

    // --- Throwing variants. Same relationship as Create vs TryCreate: ---
    // --- these delegate to the framework's Parse methods, which throw ---
    // --- FormatException / OverflowException / ArgumentNullException. ---

    /// <summary>
    /// Returns a <see cref="NonEmptyString"/> wrapping <paramref name="s"/>.
    /// Throws <see cref="ArgumentException"/> when <paramref name="s"/> is null,
    /// empty, or whitespace.
    /// </summary>
    public static NonEmptyString ToNonEmpty(this string? s) => NonEmptyString.Create(s);

    public static byte ToByte(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        byte.Parse(s!, style, format);

    public static short ToShort(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        short.Parse(s!, style, format);

    public static int ToInt(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        int.Parse(s!, style, format);

    public static long ToLong(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Integer) =>
        long.Parse(s!, style, format);

    public static float ToFloat(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        float.Parse(s!, style, format);

    public static double ToDouble(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands) =>
        double.Parse(s!, style, format);

    public static decimal ToDecimal(this string? s, IFormatProvider? format = null, NumberStyles style = NumberStyles.Number) =>
        decimal.Parse(s!, style, format);

    public static bool ToBool(this string? s) => bool.Parse(s!);

    public static DateTime ToDateTime(this string? s, IFormatProvider? format = null, DateTimeStyles style = DateTimeStyles.None) =>
        DateTime.Parse(s!, format, style);

    public static TimeSpan ToTimeSpan(this string? s, IFormatProvider? format = null) =>
        TimeSpan.Parse(s!, format);

    public static Guid ToGuid(this string? s) => Guid.Parse(s!);

    public static Guid ToGuidExact(this string? s, string format = "D") => Guid.ParseExact(s!, format);

    /// <summary>
    /// Parses <paramref name="s"/> as a member of <typeparamref name="TEnum"/>.
    /// Throws when parsing fails.
    /// </summary>
    public static TEnum ToEnum<TEnum>(this string? s, bool ignoreCase = false)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(s!, ignoreCase);
}
