using System.Net.Mail;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace StrongTypes.EfCore;

/// <summary>Pre-declares every strong-type member ahead of EF Core's property-discovery pass: public scalar wrappers get their value converter and public intervals map to two endpoint columns; a non-public backing property (e.g. an <c>internal</c> DDD field) is caught as it is added — a scalar wrapper via <see cref="IPropertyAddedConvention"/>, an interval via <see cref="IComplexPropertyAddedConvention"/>.</summary>
internal sealed class StrongTypesConvention : IEntityTypeAddedConvention, IPropertyAddedConvention, IComplexPropertyAddedConvention
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
            if (IntervalTypes.IsInterval(property.PropertyType))
            {
                AddIntervalColumns(entityTypeBuilder, property);
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

    // The complex-property counterpart of ProcessPropertyAdded, for a non-public interval backing property.
    public void ProcessComplexPropertyAdded(
        IConventionComplexPropertyBuilder propertyBuilder,
        IConventionContext<IConventionComplexPropertyBuilder> context)
    {
        var complexProperty = propertyBuilder.Metadata;
        if (IntervalTypes.IsInterval(complexProperty.ComplexType.ClrType))
        {
            ConfigureIntervalComplexType(complexProperty.ComplexType.Builder, complexProperty.IsNullable);
        }
    }

    private static void AddIntervalColumns(IConventionEntityTypeBuilder entityTypeBuilder, PropertyInfo property)
    {
        var unwrapped = Nullable.GetUnderlyingType(property.PropertyType);
        var complexProperty = entityTypeBuilder.ComplexProperty(property, unwrapped ?? property.PropertyType, fromDataAnnotation: false);
        ConfigureIntervalComplexType(complexProperty?.Metadata.ComplexType.Builder, unwrapped is not null);
    }

    private static void ConfigureIntervalComplexType(IConventionComplexTypeBuilder? typeBuilder, bool isNullable)
    {
        if (typeBuilder is null)
        {
            return;
        }
        // Shadow discriminator so a null property stays distinct from a stored all-NULL (unbounded) interval.
        if (isNullable)
        {
            typeBuilder.HasDiscriminator("Discriminator", typeof(string), fromDataAnnotation: false);
        }
        // The default bound mode stores no flag columns; a Stored mode via HasIntervalColumns maps them back in.
        typeBuilder.Ignore("StartInclusive", fromDataAnnotation: false);
        typeBuilder.Ignore("EndInclusive", fromDataAnnotation: false);
    }

    private static bool IsStrongType(Type clrType)
        => ResolveConverter(clrType) is not null || IntervalTypes.IsInterval(clrType);

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

/// <summary>Stores JSON-mapped intervals as <c>jsonb</c> on PostgreSQL, so the endpoint JSON-path translation is valid SQL (PostgreSQL's JSON operators do not exist for <c>text</c> columns).</summary>
internal sealed class IntervalJsonColumnTypeConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (IntervalTypes.IsInterval(property.ClrType))
                {
                    property.Builder.HasColumnType("jsonb");
                }
            }
        }
    }
}

internal sealed class StrongTypesConventionSetPlugin(IServiceProvider serviceProvider) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        var convention = new StrongTypesConvention();
        // Insert at index 0 so we run before PropertyDiscoveryConvention.
        conventionSet.EntityTypeAddedConventions.Insert(0, convention);
        conventionSet.PropertyAddedConventions.Add(convention);
        conventionSet.ComplexPropertyAddedConventions.Add(convention);
        if (serviceProvider.GetService<IDatabaseProvider>()?.Name == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            conventionSet.ModelFinalizingConventions.Add(new IntervalJsonColumnTypeConvention());
        }
        return conventionSet;
    }
}
