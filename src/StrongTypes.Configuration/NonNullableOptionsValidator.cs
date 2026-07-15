using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

/// <summary>Fails validation when a property of <typeparamref name="TOptions"/> declared non-nullable is null once bound, at any depth.</summary>
/// <typeparam name="TOptions">The options type being bound.</typeparam>
internal sealed class NonNullableOptionsValidator<TOptions>(string? name, IConfiguration section) : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? optionsName, TOptions options)
    {
        // A named registration validates only its own name; an unnamed one covers all.
        if (name is not null && name != optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        var failures = NullPropertyWalker.Collect(options, RootPath());

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private string RootPath() => section is IConfigurationSection { Path.Length: > 0 } s ? s.Path : "";
}
