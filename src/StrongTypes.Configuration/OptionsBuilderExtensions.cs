using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

public static class OptionsBuilderExtensions
{
    /// <summary>Binds <paramref name="section"/> to <typeparamref name="TOptions"/> and requires a configuration key for every non-nullable strong-type property on it.</summary>
    /// <param name="builder">The options builder to bind.</param>
    /// <param name="section">The configuration section to bind from.</param>
    /// <returns><paramref name="builder"/>, for chaining — pair with <c>ValidateOnStart()</c> to fail the host rather than the first request that reads the options.</returns>
    /// <remarks>
    /// <para>
    /// A wrapper's invariant constrains every value it can hold; it cannot make the binder assign
    /// one. An unconfigured <c>NonEmptyString</c> is therefore <c>null</c>, and an unconfigured
    /// <c>Positive&lt;int&gt;</c> is <c>1</c> — its default, an ordinary value that no <c>[Required]</c>
    /// can distinguish from a configured one. This checks the section for the key instead of
    /// checking the bound object for a null, so both cases fail.
    /// </para>
    /// <para>
    /// The declaration is the spec: <c>Positive&lt;int&gt;</c> is required, <c>Positive&lt;int&gt;?</c>
    /// is optional. Only Kalicz.StrongTypes wrappers are checked. A property in an assembly compiled
    /// without nullable reference types carries no annotation and is treated as optional.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="section"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> BindStrongTypes<TOptions>(this OptionsBuilder<TOptions> builder, IConfiguration section)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(section);

        builder.Bind(section);
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(
            new RequiredStrongTypeKeysValidator<TOptions>(builder.Name, section));

        return builder;
    }
}
