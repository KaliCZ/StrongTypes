using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

/// <summary>Fails validation when a property of <typeparamref name="TOptions"/> declared non-nullable is null once bound.</summary>
/// <typeparam name="TOptions">The options type being bound.</typeparam>
internal sealed class NonNullableOptionsValidator<TOptions>(string? name, IConfiguration section) : IValidateOptions<TOptions>
    where TOptions : class
{
    // Per-type and immutable, so this runs once in the static constructor — which also keeps
    // NullabilityInfoContext, which is not thread-safe, on a single thread.
    private static readonly PropertyInfo[] MustNotBeNull = FindNonNullableReferenceProperties();

    public ValidateOptionsResult Validate(string? optionsName, TOptions options)
    {
        // A named registration validates only its own name; an unnamed one covers all.
        if (name is not null && name != optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        List<string>? failures = null;

        foreach (var property in MustNotBeNull)
        {
            if (property.GetValue(options) is not null)
            {
                continue;
            }

            failures ??= [];
            failures.Add(
                $"'{Path(property.Name)}' is null. Configure it, give {typeof(TOptions).Name}.{property.Name} " +
                $"a default, or declare it nullable.");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private string Path(string propertyName) =>
        section is IConfigurationSection { Path.Length: > 0 } s ? $"{s.Path}:{propertyName}" : propertyName;

    /// <summary>
    /// Only a non-nullable reference property can end up in a state its own declaration forbids.
    /// </summary>
    /// <remarks>
    /// A value type is skipped because it has no invalid state to reach: an unconfigured
    /// <c>bool</c> is <c>false</c> and an unconfigured <c>Positive&lt;int&gt;</c> is <c>1</c> — a
    /// default, and a value the type is perfectly happy to hold. Only <c>null</c> in something that
    /// said it would never be null is a contradiction, and only a reference can manage it.
    /// <para>
    /// A property in an assembly compiled without nullable reference types reads as
    /// <see cref="NullabilityState.Unknown"/> and is skipped too: with no intent declared there is
    /// nothing to enforce.
    /// </para>
    /// </remarks>
    private static PropertyInfo[] FindNonNullableReferenceProperties()
    {
        var nullability = new NullabilityInfoContext();

        var result = new List<PropertyInfo>();
        foreach (var property in typeof(TOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetSetMethod() is null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            if (property.PropertyType.IsValueType)
            {
                continue;
            }
            if (nullability.Create(property).WriteState != NullabilityState.NotNull)
            {
                continue;
            }
            result.Add(property);
        }
        return [.. result];
    }
}
