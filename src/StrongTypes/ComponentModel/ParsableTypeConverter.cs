#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;

namespace StrongTypes;

/// <summary>Converts between <see cref="string"/> and <typeparamref name="T"/> via <see cref="IParsable{TSelf}"/>, honouring the culture it is handed.</summary>
/// <typeparam name="T">A strong type that implements <see cref="IParsable{TSelf}"/>.</typeparam>
/// <remarks>Invalid input surfaces as whatever <c>T.Parse</c> throws — for a Kalicz.StrongTypes wrapper that is the <see cref="ArgumentException"/> naming the broken invariant, which callers such as <c>ConfigurationBinder</c> surface as the inner exception. Apply to an open generic via <see cref="StrongTypeConverter"/> instead.</remarks>
public sealed class ParsableTypeConverter<T> : TypeConverter
    where T : IParsable<T>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <exception cref="ArgumentException"><paramref name="value"/> is a string that breaks <typeparamref name="T"/>'s invariant.</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is a string that is not in <typeparamref name="T"/>'s format.</exception>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string s ? T.Parse(s, culture) : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not T parsed)
            return base.ConvertTo(context, culture, value, destinationType);

        // Must round-trip ConvertFrom, which parses in `culture` — formatting in
        // any other culture would re-parse to a different number.
        return parsed is IFormattable formattable ? formattable.ToString(null, culture) : parsed.ToString();
    }
}
