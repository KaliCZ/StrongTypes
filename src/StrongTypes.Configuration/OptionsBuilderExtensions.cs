using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

public static class OptionsBuilderExtensions
{
    /// <summary>Binds <paramref name="section"/> to <typeparamref name="TOptions"/> and requires a configuration key for every property that declares it expects a value.</summary>
    /// <param name="builder">The options builder to bind.</param>
    /// <param name="section">The configuration section to bind from.</param>
    /// <returns><paramref name="builder"/>, for chaining — pair with <c>ValidateOnStart()</c> to fail the host rather than the first request that reads the options.</returns>
    /// <remarks>
    /// <para>
    /// A property is required when it is not nullable and the options class gives it no default of
    /// its own. Everything else is optional:
    /// </para>
    /// <code>
    /// public NonEmptyString Name { get; set; } = null!;          // required — null! declares no default
    /// public NonEmptyString? Nickname { get; set; }              // optional — nullable
    /// public Positive&lt;int&gt; MaxRetries { get; set; }             // required
    /// public Positive&lt;int&gt;? Score { get; set; }                 // optional — nullable
    /// public string Endpoint { get; set; } = "https://x.test";   // optional — has a default
    /// </code>
    /// <para>
    /// Every property type is checked, not only Kalicz.StrongTypes wrappers: opting in says the
    /// options class should be fully configured, and a missing <c>string</c> is as silent as a
    /// missing <c>NonEmptyString</c>.
    /// </para>
    /// <para>
    /// This is about the key that is <em>absent</em>. A wrapper's invariant constrains every value
    /// it can hold; it cannot make the binder assign one, so an unconfigured <c>NonEmptyString</c>
    /// is <c>null</c> and an unconfigured <c>Positive&lt;int&gt;</c> is <c>1</c> — its default, which
    /// no <c>[Required]</c> can tell from a configured <c>1</c>. This checks the section for each
    /// key instead of checking the bound object for a null, so both cases fail. A key that is
    /// present but invalid still fails while binding, with the invariant's own message.
    /// </para>
    /// <para>
    /// Two declarations cannot be read and are treated as required: a value type whose intended
    /// default is the CLR default (<c>bool Enabled { get; set; } = false</c>), and — for the
    /// nullable half of the rule — a reference property in an assembly compiled without nullable
    /// reference types, which carries no annotation and is instead treated as optional. Declare a
    /// property nullable when it is optional and neither applies.
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
            new RequiredConfigurationKeysValidator<TOptions>(builder.Name, section));

        return builder;
    }
}
