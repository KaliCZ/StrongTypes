using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace StrongTypes.EfCore;

/// <summary>
/// Registers value converters for every strong type against a
/// <see cref="ModelConfigurationBuilder"/>. Call from your DbContext's
/// <c>ConfigureConventions</c> override — EF Core reads these
/// pre-conventions before any property is discovered, so wrappers like
/// <see cref="NonEmptyString"/> get mapped to their underlying columns
/// instead of being treated as owned types.
/// </summary>
/// <remarks>
/// Pair with
/// <see cref="StrongTypesDbContextOptionsBuilderExtensions.UseStrongTypes"/>
/// on the <c>DbContextOptionsBuilder</c> — that one wires in the
/// <see cref="UnwrapMethodCallTranslator"/> so <c>Unwrap()</c> in LINQ
/// translates to SQL. The two hooks live in different EF Core phases
/// (pre-convention property-type config vs. internal service provider),
/// so they can't be collapsed into a single call.
/// </remarks>
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
