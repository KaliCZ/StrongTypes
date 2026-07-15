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
    }

    private sealed class MixedOptions
    {
        public NonEmptyString Name { get; set; } = null!;
        public string PlainString { get; set; } = null!;
        public string? OptionalString { get; set; }
        public int PlainInt { get; set; }
        public int? OptionalInt { get; set; }
        public string WithDefault { get; set; } = "https://example.test";
        public Positive<int> Retries { get; set; } = Positive<int>.Create(3);
    }

    /// <summary>No initialisers, so both are required and the presence check actually runs on them — which is the only way to exercise a section that has children rather than a value.</summary>
    private sealed class RequiredCollectionOptions
    {
        public List<string> Items { get; set; } = null!;
        public NestedOptions Nested { get; set; } = null!;
    }

    private sealed class NestedOptions
    {
        public string Inner { get; set; } = "";
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

    /// <summary>The failure has to name the config path, not just the property — that is what the reader goes and edits — and both ways out of it.</summary>
    [Fact]
    public void Failure_NamesTheConfigurationPathAndBothFixes()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "Name": "checkout", "Tier": 7 } }""");
        var failure = Assert.Single(exception.Failures);

        Assert.Contains("Retry:MaxRetries", failure, StringComparison.Ordinal);
        Assert.Contains("RetryOptions.MaxRetries", failure, StringComparison.Ordinal);
        Assert.Contains("a default", failure, StringComparison.Ordinal);
        Assert.Contains("declare it nullable", failure, StringComparison.Ordinal);
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

    // ── Every property type, not only the wrappers ──────────────────────
    //
    // Opting in says the options class should be fully configured: an unconfigured
    // string is exactly as silent as an unconfigured NonEmptyString.

    [Fact]
    public void MissingPlainProperties_AreRequiredToo()
    {
        var exception = BindExpectingFailure<MixedOptions>("""{ "Retry": { "Name": "checkout" } }""");
        var failures = string.Join(" | ", exception.Failures);

        Assert.Contains("Retry:PlainString", failures, StringComparison.Ordinal);
        Assert.Contains("Retry:PlainInt", failures, StringComparison.Ordinal);
    }

    [Fact]
    public void NullablePlainProperties_AreOptional()
    {
        var exception = BindExpectingFailure<MixedOptions>("""{ "Retry": { "Name": "checkout" } }""");
        var failures = string.Join(" | ", exception.Failures);

        Assert.DoesNotContain("OptionalString", failures, StringComparison.Ordinal);
        Assert.DoesNotContain("OptionalInt", failures, StringComparison.Ordinal);
    }

    /// <summary>A property the options class initialises has a stated fallback, so configuration is not required to supply one.</summary>
    [Fact]
    public void PropertiesWithADeclaredDefault_AreOptional()
    {
        var exception = BindExpectingFailure<MixedOptions>("""{ "Retry": { "Name": "checkout" } }""");
        var failures = string.Join(" | ", exception.Failures);

        Assert.DoesNotContain("WithDefault", failures, StringComparison.Ordinal);
        Assert.DoesNotContain("Retries", failures, StringComparison.Ordinal);
    }

    /// <summary><c>= null!</c> appeases nullable reference types without declaring a fallback, so the property is still required.</summary>
    [Fact]
    public void NullBangInitialiser_IsNotADeclaredDefault()
    {
        var exception = BindExpectingFailure<MixedOptions>("""{ "Retry": { "PlainInt": 1 } }""");

        Assert.Contains("Retry:Name", string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    /// <summary>
    /// A configured collection or nested object holds no scalar value of its own: the JSON array
    /// flattens to <c>Retry:Items:0</c> / <c>Retry:Items:1</c> beneath a valueless <c>Retry:Items</c>
    /// parent. Asking whether the section has a <c>Value</c> therefore calls this plainly-configured
    /// pair missing; asking whether it <c>Exists()</c> does not.
    /// </summary>
    [Fact]
    public void RequiredCollectionAndNestedObject_ConfiguredAsChildren_AreSeenAsConfigured()
    {
        var options = Bind<RequiredCollectionOptions>("""
            { "Retry": { "Items": [ "a", "b" ], "Nested": { "Inner": "y" } } }
            """);

        Assert.Equal(["a", "b"], options.Items);
        Assert.Equal("y", options.Nested.Inner);
    }

    /// <summary>The counterpart: <c>Exists()</c> must not simply wave everything through.</summary>
    [Fact]
    public void RequiredCollectionAndNestedObject_WhenActuallyAbsent_Fail()
    {
        var exception = BindExpectingFailure<RequiredCollectionOptions>("""{ "Retry": { "Unrelated": 1 } }""");
        var failures = string.Join(" | ", exception.Failures);

        Assert.Contains("Retry:Items", failures, StringComparison.Ordinal);
        Assert.Contains("Retry:Nested", failures, StringComparison.Ordinal);
    }

    /// <summary>
    /// An empty array is a deliberate configuration — the provider records it as an empty value
    /// rather than nothing — so the key is present and the requirement is met. The binder still
    /// leaves the property null rather than building an empty list: this validates that the section
    /// was configured, which is not the same question as whether the bound value is non-null, and
    /// the two only coincide for a scalar.
    /// </summary>
    [Fact]
    public void RequiredCollection_ConfiguredEmpty_SatisfiesPresenceButBindsNull()
    {
        var options = Bind<RequiredCollectionOptions>("""
            { "Retry": { "Items": [], "Nested": { "Inner": "y" } } }
            """);

        Assert.Null(options.Items);
    }

    /// <summary>An empty object holds neither value nor children, so it says nothing and reads as absent.</summary>
    [Fact]
    public void RequiredNestedObject_ConfiguredEmpty_ReadsAsAbsent()
    {
        var exception = BindExpectingFailure<RequiredCollectionOptions>("""
            { "Retry": { "Items": [ "a" ], "Nested": {} } }
            """);

        Assert.Contains("Retry:Nested", string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    /// <summary>
    /// An explicit <c>null</c> does not satisfy a required property. That also closes the hole plain
    /// binding leaves, where <c>"Name": null</c> quietly nulls a non-nullable <c>NonEmptyString</c>
    /// because nullability is erased before the binder runs.
    /// </summary>
    [Fact]
    public void RequiredProperty_ExplicitNull_Fails()
    {
        var exception = BindExpectingFailure<RetryOptions>("""
            { "Retry": { "Name": null, "MaxRetries": 5, "Tier": 7 } }
            """);

        Assert.Contains("Retry:Name", string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void MixedOptions_FullyConfigured_Binds()
    {
        var options = Bind<MixedOptions>("""{ "Retry": { "Name": "checkout", "PlainString": "s", "PlainInt": 42 } }""");

        Assert.Equal("checkout", options.Name.Value);
        Assert.Equal("s", options.PlainString);
        Assert.Equal(42, options.PlainInt);
        Assert.Equal("https://example.test", options.WithDefault);
        Assert.Equal(3, options.Retries.Value);
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
