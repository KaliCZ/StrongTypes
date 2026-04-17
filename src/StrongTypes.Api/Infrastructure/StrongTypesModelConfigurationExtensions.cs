using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Api.Infrastructure;

/// <summary>
/// Bundles every strong-type EF Core value converter behind a single call so
/// DbContexts don't have to enumerate strong types property-by-property or
/// type-by-type. Add a new converter here once and every DbContext that calls
/// <see cref="UseStrongTypes"/> picks it up automatically.
/// </summary>
public static class StrongTypesModelConfigurationExtensions
{
    public static ModelConfigurationBuilder UseStrongTypes(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<NonEmptyString>().HaveConversion<NonEmptyStringValueConverter>();

        RegisterNumeric<int>(configurationBuilder);
        RegisterNumeric<long>(configurationBuilder);
        RegisterNumeric<short>(configurationBuilder);
        RegisterNumeric<decimal>(configurationBuilder);
        RegisterNumeric<float>(configurationBuilder);
        RegisterNumeric<double>(configurationBuilder);

        return configurationBuilder;
    }


    // One call per underlying numeric type registers all four wrapper shapes
    // (Positive, NonNegative, Negative, NonPositive) against the generic
    // NumericStrongTypeValueConverter<TWrapper, T>. Adding a new underlying
    // type is a one-liner in UseStrongTypes above.
    private static void RegisterNumeric<T>(ModelConfigurationBuilder configurationBuilder)
        where T : struct, INumber<T>
    {
        configurationBuilder.Properties<Positive<T>>()
            .HaveConversion<NumericStrongTypeValueConverter<Positive<T>, T>>();
        configurationBuilder.Properties<NonNegative<T>>()
            .HaveConversion<NumericStrongTypeValueConverter<NonNegative<T>, T>>();
        configurationBuilder.Properties<Negative<T>>()
            .HaveConversion<NumericStrongTypeValueConverter<Negative<T>, T>>();
        configurationBuilder.Properties<NonPositive<T>>()
            .HaveConversion<NumericStrongTypeValueConverter<NonPositive<T>, T>>();
    }
}
