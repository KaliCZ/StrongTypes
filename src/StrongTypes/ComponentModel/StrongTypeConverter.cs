#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;

namespace StrongTypes;

/// <summary>Converts between <see cref="string"/> and a strong type that implements <see cref="IParsable{TSelf}"/>, for generic types that cannot name a closed <see cref="ParsableTypeConverter{T}"/> in an attribute argument.</summary>
public sealed class StrongTypeConverter : TypeConverter
{
    private readonly TypeConverter _inner;

    /// <param name="type">The closed strong type to convert.</param>
    /// <exception cref="ArgumentException"><paramref name="type"/> does not implement <see cref="IParsable{TSelf}"/>.</exception>
    public StrongTypeConverter(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!typeof(IParsable<>).MakeGenericType(type).IsAssignableFrom(type))
            throw new ArgumentException($"{type} must implement IParsable<{type.Name}> to be converted from a string.", nameof(type));

        _inner = (TypeConverter)Activator.CreateInstance(typeof(ParsableTypeConverter<>).MakeGenericType(type))!;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => _inner.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => _inner.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => _inner.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => _inner.ConvertTo(context, culture, value, destinationType);
}
