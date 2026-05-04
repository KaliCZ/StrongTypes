using System;
using System.ComponentModel;
using System.Globalization;

namespace StrongTypes.Wpf;

/// <summary>A <see cref="TypeConverter"/> that converts between <see cref="string"/> and <typeparamref name="T"/> via <see cref="IParsable{T}"/>. Throws the same exception that <c>T.Parse</c> throws when the input cannot be parsed; pair the binding with <c>ValidatesOnExceptions=True</c> to surface that as a WPF <c>ValidationError</c>.</summary>
/// <typeparam name="T">A strong type that implements <see cref="IParsable{T}"/>.</typeparam>
public sealed class ParsableTypeConverter<T> : TypeConverter where T : IParsable<T>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string s ? T.Parse(s, culture) : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string) && value is T t
            ? t.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
}
