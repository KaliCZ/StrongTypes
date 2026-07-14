using System;
using System.Collections.Generic;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>Without a <c>TypeConverter</c>, <c>ConfigurationBinder</c> cannot turn <c>"5"</c> into a strong type and silently leaves the property at its default rather than failing — so these assert the bound value, not merely that binding returned.</summary>
public class OptionsBindingTests
{
    private sealed class RetryOptions
    {
        public Positive<int> MaxRetries { get; set; }
        public NonNegative<int> InitialDelaySeconds { get; set; }
        public NonEmptyString? Name { get; set; }
        public Email? Contact { get; set; }
        public Digit Tier { get; set; }
        public Positive<int>? OptionalLimit { get; set; }
    }

    private static IConfiguration Config(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>();
        foreach (var (key, value) in settings)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static RetryOptions BindViaOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(configuration.GetSection("Retry"));
        return services.BuildServiceProvider().GetRequiredService<IOptions<RetryOptions>>().Value;
    }

    // ── The scenario the converter unlocks ──────────────────────────────

    [Fact]
    public void EveryStrongTypeBindsFromConfiguration()
    {
        var options = BindViaOptions(Config(
            ("Retry:MaxRetries", "5"),
            ("Retry:InitialDelaySeconds", "0"),
            ("Retry:Name", "checkout"),
            ("Retry:Contact", "ops@example.com"),
            ("Retry:Tier", "7"),
            ("Retry:OptionalLimit", "99")));

        Assert.Equal(5, options.MaxRetries.Value);
        Assert.Equal(0, options.InitialDelaySeconds.Value);
        Assert.Equal("checkout", options.Name?.Value);
        Assert.Equal("ops@example.com", options.Contact?.Address);
        Assert.Equal(7, options.Tier.Value);
        Assert.Equal(99, options.OptionalLimit?.Value);
    }

    /// <summary><c>default(Positive&lt;int&gt;)</c> is a plausible-looking <c>1</c>, so a dropped config value reads as a real one — hence asserting the bound value differs from the default.</summary>
    [Property]
    public void BoundValueComesFromConfiguration_NotTheDefault(int configured)
    {
        if (configured <= 0 || configured == default(Positive<int>).Value) return;

        var options = BindViaOptions(Config(("Retry:MaxRetries", configured.ToString())));

        Assert.Equal(configured, options.MaxRetries.Value);
        Assert.NotEqual(default(Positive<int>).Value, options.MaxRetries.Value);
    }

    [Fact]
    public void AbsentKeyLeavesTheDefault()
    {
        var options = BindViaOptions(Config(("Retry:Name", "checkout")));

        Assert.Equal(default(Positive<int>).Value, options.MaxRetries.Value);
        Assert.Null(options.OptionalLimit);
    }

    // ── Invalid configuration fails, and says why ───────────────────────

    [Theory]
    [InlineData("Retry:MaxRetries", "-5", "must be positive")]
    [InlineData("Retry:MaxRetries", "0", "must be positive")]
    [InlineData("Retry:InitialDelaySeconds", "-1", "must be non-negative")]
    [InlineData("Retry:Name", " ", "whitespace")]
    [InlineData("Retry:Contact", "not-an-email", "valid email")]
    [InlineData("Retry:Tier", "42", "single decimal digit")]
    public void ValueBreakingTheInvariant_ThrowsAndNamesTheReason(string key, string value, string expectedReason)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => BindViaOptions(Config((key, value))));

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);

        var inner = Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Contains(expectedReason, inner.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Retry:MaxRetries", "not-a-number")]
    [InlineData("Retry:Tier", "not-a-digit")]
    public void ValueInTheWrongFormat_Throws(string key, string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => BindViaOptions(Config((key, value))));
        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A nullable wrapper still enforces the invariant when a value is present.</summary>
    [Fact]
    public void NullableWrapper_InvalidValue_Throws() =>
        Assert.Throws<InvalidOperationException>(() => BindViaOptions(Config(("Retry:OptionalLimit", "-1"))));

    /// <summary>
    /// Binding is lazy: the failure surfaces on first <c>IOptions.Value</c>, not at
    /// <c>BuildServiceProvider</c>. Callers wanting startup failure add <c>ValidateOnStart</c>.
    /// </summary>
    [Fact]
    public void InvalidValue_DoesNotThrowUntilOptionsAreRead()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(Config(("Retry:MaxRetries", "-5")).GetSection("Retry"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IOptions<RetryOptions>>();

        Assert.Throws<InvalidOperationException>(() => accessor.Value);
    }
}
