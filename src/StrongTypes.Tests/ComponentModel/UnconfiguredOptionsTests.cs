using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>What a strong type holds when its key is simply not in config.</summary>
public class UnconfiguredOptionsTests
{
    private sealed class RetryOptions
    {
        public NonEmptyString Name { get; set; } = null!;
        public Positive<int> MaxRetries { get; set; }
        public Positive<int>? MaxRetriesOrUnset { get; set; }
        public Digit Tier { get; set; }
    }

    private sealed class RequiredRetryOptions
    {
        [Required] public NonEmptyString Name { get; set; } = null!;
        [Required] public Positive<int> MaxRetries { get; set; }
        [Required] public Positive<int>? MaxRetriesOrUnset { get; set; }
        public Digit Tier { get; set; }
    }

    /// <summary>The section exists so the binder produces an instance rather than returning null.</summary>
    private const string OnlyTierConfigured = "{\"Retry\":{\"Tier\":7}}";

    private static IConfigurationSection Section(string json) =>
        new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build()
            .GetSection("Retry");

    private static TOptions Bind<TOptions>(string json, bool validate = false) where TOptions : class
    {
        var services = new ServiceCollection();
        var builder = services.AddOptions<TOptions>().Bind(Section(json));
        if (validate) builder.ValidateDataAnnotations();
        return services.BuildServiceProvider().GetRequiredService<IOptions<TOptions>>().Value;
    }

    /// <summary>A non-nullable reference wrapper is <c>null</c> when unconfigured — nullability is erased before the binder runs, so it behaves exactly as a <see cref="string"/> would.</summary>
    [Fact]
    public void UnconfiguredReferenceWrapper_IsNull() =>
        Assert.Null(Bind<RetryOptions>(OnlyTierConfigured).Name);

    /// <summary>A struct wrapper falls back to <c>default</c>, which satisfies the invariant and so reads as a deliberately configured value.</summary>
    [Fact]
    public void UnconfiguredStructWrapper_IsItsDefault()
    {
        var options = Bind<RetryOptions>(OnlyTierConfigured);

        Assert.Equal(default(Positive<int>).Value, options.MaxRetries.Value);
        Assert.Equal(1, options.MaxRetries.Value);
        Assert.Equal(7, options.Tier.Value);
    }

    [Fact]
    public void UnconfiguredNullableStructWrapper_IsNull() =>
        Assert.Null(Bind<RetryOptions>(OnlyTierConfigured).MaxRetriesOrUnset);

    /// <summary>Binding alone raises nothing for a missing key, whatever the property's nullability says.</summary>
    [Fact]
    public void MissingKeys_BindWithoutError()
    {
        var options = Bind<RetryOptions>(OnlyTierConfigured);

        Assert.Null(options.Name);
        Assert.Equal(1, options.MaxRetries.Value);
    }

    /// <summary>
    /// <c>[Required]</c> plus <c>ValidateDataAnnotations()</c> catches a missing reference wrapper
    /// and a missing nullable struct wrapper — both are null — but <b>cannot</b> catch a missing
    /// non-nullable struct wrapper, whose default is an ordinary value and never null.
    /// </summary>
    [Fact]
    public void Required_CatchesNullsOnly_NotAStructsDefault()
    {
        var exception = Assert.Throws<OptionsValidationException>(
            () => Bind<RequiredRetryOptions>(OnlyTierConfigured, validate: true));

        var failures = string.Join(" | ", exception.Failures);

        Assert.Contains($"'{nameof(RequiredRetryOptions.Name)}'", failures, System.StringComparison.Ordinal);
        Assert.Contains($"'{nameof(RequiredRetryOptions.MaxRetriesOrUnset)}'", failures, System.StringComparison.Ordinal);
        Assert.DoesNotContain($"'{nameof(RequiredRetryOptions.MaxRetries)}'", failures, System.StringComparison.Ordinal);
    }

    /// <summary>Every key present and valid: validation passes, so the failures above are about absence rather than the annotations themselves.</summary>
    [Fact]
    public void Required_FullyConfigured_Passes()
    {
        var options = Bind<RequiredRetryOptions>(
            "{\"Retry\":{\"Name\":\"checkout\",\"MaxRetries\":5,\"MaxRetriesOrUnset\":9,\"Tier\":7}}",
            validate: true);

        Assert.Equal("checkout", options.Name.Value);
        Assert.Equal(5, options.MaxRetries.Value);
        Assert.Equal(9, options.MaxRetriesOrUnset?.Value);
    }

    /// <summary><c>Get&lt;T&gt;</c> on a section with no children at all returns <c>null</c> rather than a defaulted instance.</summary>
    [Fact]
    public void EmptySection_GetReturnsNull() =>
        Assert.Null(Section("{\"Retry\":{}}").Get<RetryOptions>());
}
