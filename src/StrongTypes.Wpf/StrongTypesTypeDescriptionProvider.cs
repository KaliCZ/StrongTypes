using System;
using System.ComponentModel;

namespace StrongTypes.Wpf;

internal sealed class StrongTypesTypeDescriptionProvider(TypeDescriptionProvider parent) : TypeDescriptionProvider(parent)
{
    public override ICustomTypeDescriptor? GetTypeDescriptor(Type objectType, object? instance)
    {
        var inner = base.GetTypeDescriptor(objectType, instance);
        if (inner is null)
            return null;
        var converter = TryCreateConverter(objectType);
        return converter is null ? inner : new StrongTypeDescriptor(inner, converter);
    }

    private static TypeConverter? TryCreateConverter(Type type)
    {
        if (type == typeof(NonEmptyString))
            return new ParsableTypeConverter<NonEmptyString>();
        if (type == typeof(Email))
            return new ParsableTypeConverter<Email>();
        if (type == typeof(Digit))
            return new ParsableTypeConverter<Digit>();
        if (!type.IsGenericType)
            return null;
        var definition = type.GetGenericTypeDefinition();
        if (definition != typeof(Positive<>)
            && definition != typeof(NonNegative<>)
            && definition != typeof(Negative<>)
            && definition != typeof(NonPositive<>))
            return null;
        var converterType = typeof(ParsableTypeConverter<>).MakeGenericType(type);
        return (TypeConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class StrongTypeDescriptor(ICustomTypeDescriptor parent, TypeConverter converter) : CustomTypeDescriptor(parent)
{
    public override TypeConverter GetConverter() => converter;
}
