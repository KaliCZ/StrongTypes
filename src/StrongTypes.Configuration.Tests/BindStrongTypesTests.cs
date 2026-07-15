using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrongTypes.Configuration.Tests.NullableDisabled;
using Xunit;

namespace StrongTypes.Configuration.Tests;

/// <summary>
/// <c>BindStrongTypes</c> exists for the key that isn't there. Binding an absent key succeeds
/// without assigning, so nothing is raised and the property keeps a default — <c>null</c> for a
/// reference wrapper, and for a struct wrapper an ordinary value that no <c>[Required]</c> can tell
/// from a configured one.
/// </summary>
public class BindStrongTypesTests
{
    private sealed class RetryOptions
    {
        public NonEmptyString Name { get; set; } = null!;
        public NonEmptyString? Nickname { get; set; }
        public Positive<int> MaxRetries { get; set; }
        public Positive<int>? Score { get; set; }
        public Digit Tier { get; set; }
        public string PlainString { get; set; } = null!;
        public int PlainInt { get; set; }
    }

    private const string FullyConfigured = """
        { "Retry": { "Name": "checkout", "MaxRetries": 5, "Tier": 7 } }
        """;

    private static IConfigurationSection Section(string json) =>
        new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build()
            .GetSection("Retry");

    private static TOptions Bind<TOptions>(string json) where TOptions : class
    {
        var services = new ServiceCollection();
        services.AddOptions<TOptions>().BindStrongTypes(Section(json));
        return services.BuildServiceProvider().GetRequiredService<IOptions<TOptions>>().Value;
    }

    private static OptionsValidationException BindExpectingFailure<TOptions>(string json) where TOptions : class =>
        Assert.Throws<OptionsValidationException>(() => Bind<TOptions>(json));

    // ── The gap it closes ───────────────────────────────────────────────

    /// <summary>The contrast, and the whole reason the package exists: plain <c>Bind</c> takes the same config without a murmur, leaving a null <c>NonEmptyString</c> and a <c>MaxRetries</c> of 1 that reads as deliberate.</summary>
    [Fact]
    public void PlainBind_AcceptsTheSameMissingKeysSilently()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(Section("""{ "Retry": { "Tier": 7 } }"""));

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RetryOptions>>().Value;

        Assert.Null(options.Name);
        Assert.Equal(1, options.MaxRetries.Value);
    }

    /// <summary>The case that motivated the package: <c>default(Positive&lt;int&gt;)</c> is <c>1</c>, so nothing about the bound object says "never configured".</summary>
    [Fact]
    public void MissingNonNullableStructWrapper_Fails()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "Name": "checkout", "Tier": 7 } }""");

        Assert.Contains("'Retry:MaxRetries' is required but was not configured", string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingNonNullableReferenceWrapper_Fails()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "MaxRetries": 5, "Tier": 7 } }""");

        Assert.Contains("'Retry:Name' is required but was not configured", string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void EveryMissingKey_IsReportedTogether()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "Tier": 7 } }""");

        var failures = string.Join(" | ", exception.Failures);
        Assert.Contains("Retry:Name", failures, StringComparison.Ordinal);
        Assert.Contains("Retry:MaxRetries", failures, StringComparison.Ordinal);
    }

    /// <summary>The failure has to name the config path, not the property — that is what the reader has to go and edit.</summary>
    [Fact]
    public void Failure_NamesTheConfigurationPathAndTheFix()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "Name": "checkout", "Tier": 7 } }""");
        var failure = Assert.Single(exception.Failures);

        Assert.Contains("Retry:MaxRetries", failure, StringComparison.Ordinal);
        Assert.Contains("nullable if it is optional", failure, StringComparison.Ordinal);
    }

    // ── What it must not do ─────────────────────────────────────────────

    [Fact]
    public void FullyConfigured_Binds()
    {
        var options = Bind<RetryOptions>(FullyConfigured);

        Assert.Equal("checkout", options.Name.Value);
        Assert.Equal(5, options.MaxRetries.Value);
        Assert.Equal(7, options.Tier.Value);
    }

    [Fact]
    public void MissingNullableWrappers_DoNotFail()
    {
        var options = Bind<RetryOptions>(FullyConfigured);

        Assert.Null(options.Nickname);
        Assert.Null(options.Score);
    }

    /// <summary>Only our wrappers are policed; an unconfigured <c>string</c> or <c>int</c> is left to whatever validation the caller already has.</summary>
    [Fact]
    public void MissingPlainProperties_AreNotPoliced()
    {
        var options = Bind<RetryOptions>(FullyConfigured);

        Assert.Null(options.PlainString);
        Assert.Equal(0, options.PlainInt);
    }

    /// <summary>Presence is the only question asked here; an invalid value is still the converter's failure, thrown while binding.</summary>
    [Fact]
    public void PresentButInvalidValue_StillThrowsTheInvariantFailure()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Bind<RetryOptions>("""{ "Retry": { "Name": "checkout", "MaxRetries": -5, "Tier": 7 } }"""));

        // Assert.Throws is exact-type, so reaching here already rules out a validation failure.
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    // ── Named options ───────────────────────────────────────────────────

    [Fact]
    public void NamedOptions_ValidateOnlyTheirOwnName()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>("configured").BindStrongTypes(Section(FullyConfigured));
        services.AddOptions<RetryOptions>("incomplete").BindStrongTypes(Section("""{ "Retry": { "Tier": 7 } }"""));

        var monitor = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<RetryOptions>>();

        Assert.Equal("checkout", monitor.Get("configured").Name.Value);
        Assert.Throws<OptionsValidationException>(() => monitor.Get("incomplete"));
    }

    // ── Nullable reference types disabled ───────────────────────────────

    /// <summary>
    /// An options class from an assembly compiled without nullable reference types declares no
    /// intent for a reference wrapper, so it is treated as optional rather than guessed at. The
    /// struct wrapper still carries its own distinction and is still required.
    /// </summary>
    [Fact]
    public void UnannotatedAssembly_ReferenceWrapperIsOptional_StructWrapperIsStillRequired()
    {
        var exception = BindExpectingFailure<UnannotatedOptions>("""{ "Retry": { "Tier": 7 } }""");
        var failures = string.Join(" | ", exception.Failures);

        Assert.Contains("Retry:MaxRetries", failures, StringComparison.Ordinal);
        Assert.DoesNotContain("Retry:Name", failures, StringComparison.Ordinal);
    }

    // ── Guard rails ─────────────────────────────────────────────────────

    [Fact]
    public void NullSection_Throws()
    {
        var builder = new ServiceCollection().AddOptions<RetryOptions>();

        Assert.Throws<ArgumentNullException>(() => builder.BindStrongTypes(null!));
    }
}
