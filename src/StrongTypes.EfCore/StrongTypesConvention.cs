using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>Pre-declares every strong-type member as a scalar property with its value converter attached, ahead of EF Core's property-discovery pass.</summary>
internal sealed class StrongTypesConvention : IEntityTypeAddedConvention
{
    public void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var entityType = entityTypeBuilder.Metadata;
        var clrType = entityType.ClrType;
        // Guard: if EF ever tried to add a strong-type itself as an entity
        // type (e.g. it leaked from somewhere), don't walk its members —
        // NonEmptyString's private Value property shouldn't become a column.
        if (IsStrongType(clrType))
        {
            return;
        }
        foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetMethod is null || property.SetMethod is null)
            {
                continue;
            }
            var converter = ResolveConverter(property.PropertyType);
            if (converter is null)
            {
                continue;
            }
            var propertyBuilder = entityTypeBuilder.Property(property.PropertyType, property.Name);
            propertyBuilder?.HasConversion(converter);
        }
    }

    private static bool IsStrongType(Type clrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (unwrapped == typeof(NonEmptyString))
        {
            return true;
        }
        if (unwrapped.IsGenericType)
        {
            var definition = unwrapped.GetGenericTypeDefinition();
            return definition == typeof(Positive<>)
                || definition == typeof(NonNegative<>)
                || definition == typeof(Negative<>)
                || definition == typeof(NonPositive<>)
                || definition == typeof(BoundedInt<>);
        }
        return false;
    }

    // EF Core applies ValueConverter only to non-null values, so the same
    // converter instance works for both Wrapper and Nullable<Wrapper> (and for
    // NonEmptyString vs NonEmptyString?, which are the same CLR type anyway).
    private static ValueConverter? ResolveConverter(Type clrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (unwrapped == typeof(NonEmptyString))
        {
            return new NonEmptyStringValueConverter();
        }
        if (unwrapped.IsGenericType)
        {
            var definition = unwrapped.GetGenericTypeDefinition();
            if (definition == typeof(Positive<>) ||
                definition == typeof(NonNegative<>) ||
                definition == typeof(Negative<>) ||
                definition == typeof(NonPositive<>) ||
                definition == typeof(BoundedInt<>))
            {
                // Read the underlying numeric type from the wrapper's Value
                // property: BoundedInt<TBounds> closes over the bounds witness,
                // so its first generic argument isn't the underlying type.
                var underlying = unwrapped.GetProperty("Value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!.PropertyType;
                var converterType = typeof(NumericStrongTypeValueConverter<,>).MakeGenericType(unwrapped, underlying);
                return (ValueConverter)Activator.CreateInstance(converterType)!;
            }
        }
        return null;
    }
}

internal sealed class StrongTypesConventionSetPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        // Insert at index 0 so we run before PropertyDiscoveryConvention.
        conventionSet.EntityTypeAddedConventions.Insert(0, new StrongTypesConvention());
        return conventionSet;
    }
}
