using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

/// <summary>Fails validation when a non-nullable strong-type property on <typeparamref name="TOptions"/> has no key in the bound section.</summary>
/// <typeparam name="TOptions">The options type being bound.</typeparam>
internal sealed class RequiredStrongTypeKeysValidator<TOptions>(string? name, IConfiguration section) : IValidateOptions<TOptions>
    where TOptions : class
{
    private static readonly NullabilityInfoContext NullabilityContext = new();
    private static readonly Lock NullabilityGate = new();

    public ValidateOptionsResult Validate(string? optionsName, TOptions options)
    {
        // A named registration validates only its own name; Configure(null) covers all.
        if (name is not null && name != optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        List<string>? failures = null;

        foreach (var property in typeof(TOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetSetMethod() is null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            if (!StrongTypeProperty.IsStrongType(property.PropertyType) || StrongTypeProperty.IsOptional(property, Nullability))
            {
                continue;
            }
            if (section.GetSection(property.Name).Value is not null)
            {
                continue;
            }

            failures ??= [];
            failures.Add($"'{Path(property.Name)}' is required but was not configured. Declare {typeof(TOptions).Name}.{property.Name} nullable if it is optional.");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private string Path(string propertyName) =>
        string.IsNullOrEmpty(section is IConfigurationSection s ? s.Path : null)
            ? propertyName
            : $"{((IConfigurationSection)section).Path}:{propertyName}";

    /// <summary><see cref="NullabilityInfoContext"/> is documented as not thread-safe, and options validation can run concurrently.</summary>
    private static NullabilityInfo Nullability(PropertyInfo property)
    {
        lock (NullabilityGate)
        {
            return NullabilityContext.Create(property);
        }
    }
}
