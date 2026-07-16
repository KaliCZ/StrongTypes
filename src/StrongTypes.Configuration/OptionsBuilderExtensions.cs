using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

public static class OptionsBuilderExtensions
{
    /// <summary>
    /// Binds <paramref name="section"/> to <typeparamref name="TOptions"/>, then fails validation when a
    /// non-nullable reference property is left null at any depth. Value types are never required — an
    /// unconfigured <c>Positive&lt;int&gt;</c> is <c>1</c>. Pair with <c>ValidateOnStart()</c> to fail at
    /// startup rather than on first read.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="section"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> BindStrongTypes<TOptions>(this OptionsBuilder<TOptions> builder, IConfiguration section)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(section);

        builder.Bind(section);
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(
            new NonNullableOptionsValidator<TOptions>(builder.Name, section));

        return builder;
    }
}
