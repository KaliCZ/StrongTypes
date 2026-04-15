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
        return configurationBuilder;
    }
}
