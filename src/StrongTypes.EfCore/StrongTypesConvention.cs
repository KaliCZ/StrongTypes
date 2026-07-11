using System.Net.Mail;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>Attaches the right value converter to every strong-type property: public ones are pre-declared ahead of EF Core's property-discovery pass; non-public ones (e.g. an <c>internal</c> DDD backing property) are caught as they are added.</summary>
internal sealed class StrongTypesConvention : IEntityTypeAddedConvention, IPropertyAddedConvention
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

    // ProcessEntityTypeAdded only scans public properties; a non-public mapped
    // property (an internal/private DDD backing field) reaches the convention here.
    public void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        var converter = ResolveConverter(propertyBuilder.Metadata.ClrType);
        if (converter is not null)
        {
            propertyBuilder.HasConversion(converter);
        }
    }

    private static bool IsStrongType(Type clrType) => ResolveConverter(clrType) is not null;

    // EF Core applies ValueConverter only to non-null values, so the same
    // converter instance works for both Wrapper and Nullable<Wrapper> (and for
    // NonEmptyString vs NonEmptyString?, which are the same CLR type anyway).
    private static ValueConverter? ResolveConverter(Type clrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (unwrapped == typeof(NonEmptyString))
            return new NonEmptyStringValueConverter();
        if (unwrapped == typeof(Email))
            return new EmailValueConverter();
        if (unwrapped == typeof(MailAddress))
            return new MailAddressValueConverter();
        if (unwrapped.IsGenericType && IsNumericWrapper(unwrapped))
            return CreateNumericConverter(unwrapped);
        return null;
    }

    private static bool IsNumericWrapper(Type unwrapped)
    {
        var definition = unwrapped.GetGenericTypeDefinition();
        return definition == typeof(Positive<>)
            || definition == typeof(NonNegative<>)
            || definition == typeof(Negative<>)
            || definition == typeof(NonPositive<>);
    }

    private static ValueConverter CreateNumericConverter(Type unwrapped)
    {
        var underlying = unwrapped.GetGenericArguments()[0];
        var converterType = typeof(NumericStrongTypeValueConverter<,>).MakeGenericType(unwrapped, underlying);
        return (ValueConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class StrongTypesConventionSetPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        var convention = new StrongTypesConvention();
        // Insert at index 0 so we run before PropertyDiscoveryConvention.
        conventionSet.EntityTypeAddedConventions.Insert(0, convention);
        conventionSet.PropertyAddedConventions.Add(convention);
        return conventionSet;
    }
}
