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

        configurationBuilder.Properties<Positive<int>>()
            .HaveConversion<NumericStrongTypeValueConverter<Positive<int>, int>>();
        configurationBuilder.Properties<NonNegative<int>>()
            .HaveConversion<NumericStrongTypeValueConverter<NonNegative<int>, int>>();
        configurationBuilder.Properties<Negative<int>>()
            .HaveConversion<NumericStrongTypeValueConverter<Negative<int>, int>>();
        configurationBuilder.Properties<NonPositive<int>>()
            .HaveConversion<NumericStrongTypeValueConverter<NonPositive<int>, int>>();

        return configurationBuilder;
    }
}
