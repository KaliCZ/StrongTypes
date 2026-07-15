using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

/// <summary>Fails validation when a property of <typeparamref name="TOptions"/> that must be configured has no key in the bound section.</summary>
/// <typeparam name="TOptions">The options type being bound.</typeparam>
internal sealed class RequiredConfigurationKeysValidator<TOptions>(string? name, IConfiguration section) : IValidateOptions<TOptions>
    where TOptions : class
{
    // Per-type and immutable, so this runs once in the static constructor —
    // which also keeps NullabilityInfoContext, which is not thread-safe, on one thread.
    private static readonly PropertyInfo[] RequiredProperties = FindPropertiesRequiringConfiguration();

    public ValidateOptionsResult Validate(string? optionsName, TOptions options)
    {
        // A named registration validates only its own name; an unnamed one covers all.
        if (name is not null && name != optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        List<string>? failures = null;

        foreach (var property in RequiredProperties)
        {
            // Exists() rather than a null Value: a collection or nested object is a section
            // with children and no value of its own, and is configured all the same.
            if (section.GetSection(property.Name).Exists())
            {
                continue;
            }

            failures ??= [];
            failures.Add(
                $"'{Path(property.Name)}' is required but was not configured. Give {typeof(TOptions).Name}.{property.Name} " +
                $"a default, or declare it nullable, if it is optional.");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private string Path(string propertyName) =>
        section is IConfigurationSection { Path.Length: > 0 } s ? $"{s.Path}:{propertyName}" : propertyName;

    /// <summary>
    /// A property must be configured when its declaration says a value is expected — it is not
    /// nullable — and the options class supplies none of its own.
    /// </summary>
    private static PropertyInfo[] FindPropertiesRequiringConfiguration()
    {
        var declaredDefaults = TryConstructProbe();
        var nullability = new NullabilityInfoContext();

        var required = new List<PropertyInfo>();
        foreach (var property in typeof(TOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetSetMethod() is null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            if (IsOptional(property, nullability) || SuppliesItsOwnDefault(property, declaredDefaults))
            {
                continue;
            }
            required.Add(property);
        }
        return [.. required];
    }

    /// <summary>A fresh instance, to read what the options class declares before configuration touches it.</summary>
    private static TOptions? TryConstructProbe()
    {
        try
        {
            return Activator.CreateInstance<TOptions>();
        }
        catch (Exception e) when (e is MissingMethodException or MemberAccessException or TargetInvocationException)
        {
            // Binding needs a parameterless constructor anyway, so this is unreachable in practice
            // and would already have failed. Treating every property as having no default is the
            // strict reading, and the failure it produces names the key either way.
            return null;
        }
    }

    /// <summary>
    /// True when the options class initialises the property to anything other than the CLR default,
    /// which is the only way it can say "optional, and here is the fallback".
    /// </summary>
    /// <remarks>
    /// A value type whose intended default <em>is</em> the CLR default — <c>bool Enabled { get; set; } = false</c>
    /// — is indistinguishable from one with no initialiser at all, and so is required. Declare it
    /// nullable to make it optional.
    /// </remarks>
    private static bool SuppliesItsOwnDefault(PropertyInfo property, TOptions? declaredDefaults)
    {
        if (declaredDefaults is null)
        {
            return false;
        }

        var declared = property.GetValue(declaredDefaults);
        var clrDefault = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
        return !Equals(declared, clrDefault);
    }

    /// <remarks>
    /// A reference property in an assembly compiled without nullable reference types carries no
    /// annotation, so it reads as <see cref="NullabilityState.Unknown"/> and is treated as optional:
    /// with no intent declared there is nothing to enforce.
    /// </remarks>
    private static bool IsOptional(PropertyInfo property, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }
        if (property.PropertyType.IsValueType)
        {
            return false;
        }
        return nullability.Create(property).WriteState != NullabilityState.NotNull;
    }
}
