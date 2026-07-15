using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrongTypes.Configuration;

public static class OptionsBuilderExtensions
{
    /// <summary>Binds <paramref name="section"/> to <typeparamref name="TOptions"/> and fails when a property declared non-nullable is null once bound.</summary>
    /// <param name="builder">The options builder to bind.</param>
    /// <param name="section">The configuration section to bind from.</param>
    /// <returns><paramref name="builder"/>, for chaining — pair with <c>ValidateOnStart()</c> to fail the host rather than the first request that reads the options.</returns>
    /// <remarks>
    /// <para>
    /// The binder assigns nothing for an absent key, so a property it never reaches keeps whatever
    /// the options class gave it — and a <c>NonEmptyString</c> that was given nothing is <c>null</c>,
    /// which is precisely what the type says it can never be. Nullable reference annotations already
    /// state which properties that applies to, so no attribute has to repeat it:
    /// </para>
    /// <code>
    /// public NonEmptyString Name { get; set; } = null!;          // fails unless configured
    /// public NonEmptyString? Nickname { get; set; }              // nullable — fine
    /// public string Endpoint { get; set; } = "https://x.test";   // has a default — never null
    /// </code>
    /// <para>
    /// Every reference property is covered, not only Kalicz.StrongTypes wrappers: a <c>string</c>
    /// declared non-nullable is as broken by a missing key as a <c>NonEmptyString</c> is.
    /// </para>
    /// <para>
    /// A value type is not checked, because it has no invalid state to reach — an unconfigured
    /// <c>Positive&lt;int&gt;</c> is <c>1</c> and an unconfigured <c>bool</c> is <c>false</c>, both
    /// values those types are happy to hold. If "not configured" has to be distinguishable from a
    /// configured default, declare it nullable (<c>Positive&lt;int&gt;?</c>) and check for null
    /// yourself.
    /// </para>
    /// <para>
    /// This is about the key that is absent. A key that is present but invalid still fails while
    /// binding, with the invariant's own message.
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
            new NonNullableOptionsValidator<TOptions>(builder.Name, section));

        return builder;
    }
}
