using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrongTypes.Configuration.Tests.NullableDisabled;
using Xunit;

namespace StrongTypes.Configuration.Tests;

/// <summary>
/// The binder assigns nothing for an absent key, so a non-nullable property it never reaches keeps
/// the null the options class gave it — the state its type forbids and plain binding never catches.
/// </summary>
public class BindStrongTypesTests
{
    private sealed class RetryOptions
    {
        public NonEmptyString Name { get; set; } = null!;
        public NonEmptyString? Nickname { get; set; }
        public Email Contact { get; set; } = null!;
        public string PlainString { get; set; } = null!;
        public string? OptionalString { get; set; }
        public string WithDefault { get; set; } = "https://example.test";
        public NestedOptions Nested { get; set; } = null!;
        public List<string> Items { get; set; } = null!;
    }

    /// <summary>Nothing here can be null, so nothing here is checked.</summary>
    private sealed class ValueTypeOptions
    {
        public Positive<int> MaxRetries { get; set; }
        public NonNegative<int> Delay { get; set; }
        public Digit Tier { get; set; }
        public bool Enabled { get; set; }
        public int Timeout { get; set; }
        public Positive<int>? Score { get; set; }
    }

    private sealed class NestedOptions
    {
        public string Inner { get; set; } = "";
    }

    private const string FullyConfigured = """
        {
          "Retry": {
            "Name": "checkout",
            "Contact": "ops@example.com",
            "PlainString": "s",
            "Nested": { "Inner": "y" },
            "Items": [ "a", "b" ]
          }
        }
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

    private static string Failures(OptionsValidationException exception) => string.Join(" | ", exception.Failures);

    // ── The gap it closes ───────────────────────────────────────────────

    [Fact]
    public void PlainBind_LeavesANonNullableWrapperNull()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(Section("""{ "Retry": { } }"""));

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RetryOptions>>().Value;

        Assert.Null(options.Name);
    }

    [Fact]
    public void MissingNonNullableWrapper_Fails()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { } }""");

        Assert.Contains("'Retry:Name' is null", Failures(exception), StringComparison.Ordinal);
        Assert.Contains("'Retry:Contact' is null", Failures(exception), StringComparison.Ordinal);
    }

    /// <summary>A plain <c>string</c> declared non-nullable is as broken by a missing key as a wrapper is.</summary>
    [Fact]
    public void MissingNonNullablePlainReference_Fails() =>
        Assert.Contains("'Retry:PlainString' is null", Failures(BindExpectingFailure<RetryOptions>("""{ "Retry": { } }""")), StringComparison.Ordinal);

    [Fact]
    public void MissingNonNullableNestedObjectAndCollection_Fail()
    {
        var failures = Failures(BindExpectingFailure<RetryOptions>("""{ "Retry": { } }"""));

        Assert.Contains("'Retry:Nested' is null", failures, StringComparison.Ordinal);
        Assert.Contains("'Retry:Items' is null", failures, StringComparison.Ordinal);
    }

    /// <summary>An explicit <c>null</c> is no better than an absent key.</summary>
    [Fact]
    public void ExplicitNull_Fails() =>
        Assert.Contains("'Retry:Name' is null", Failures(BindExpectingFailure<RetryOptions>("""{ "Retry": { "Name": null } }""")), StringComparison.Ordinal);

    [Fact]
    public void Failure_NamesTheConfigurationPathAndEveryWayOut()
    {
        var exception = BindExpectingFailure<RetryOptions>("""{ "Retry": { "Name": "checkout", "Contact": "a@b.test", "PlainString": "s", "Nested": { "Inner": "y" } } }""");

        Assert.Equal(
            "'Retry:Items' is null. Configure it, give RetryOptions.Items a default, or declare it nullable.",
            Assert.Single(exception.Failures));
    }

    // ── What it must not do ─────────────────────────────────────────────

    [Fact]
    public void FullyConfigured_Binds()
    {
        var options = Bind<RetryOptions>(FullyConfigured);

        Assert.Equal("checkout", options.Name.Value);
        Assert.Equal("ops@example.com", options.Contact.Address);
        Assert.Equal(["a", "b"], options.Items);
        Assert.Equal("y", options.Nested.Inner);
    }

    [Fact]
    public void NullableProperties_AreNotChecked()
    {
        var options = Bind<RetryOptions>(FullyConfigured);

        Assert.Null(options.Nickname);
        Assert.Null(options.OptionalString);
    }

    [Fact]
    public void PropertiesWithADeclaredDefault_NeedNoConfiguration() =>
        Assert.Equal("https://example.test", Bind<RetryOptions>(FullyConfigured).WithDefault);

    /// <summary>A value type has no null state to catch: unconfigured it holds a default the type is happy with, so none of these is ever required.</summary>
    [Fact]
    public void ValueTypeProperties_AreNeverRequired()
    {
        var options = Bind<ValueTypeOptions>("""{ "Retry": { } }""");

        Assert.Equal(1, options.MaxRetries.Value);
        Assert.Equal(0, options.Delay.Value);
        Assert.Equal(0, options.Tier.Value);
        Assert.False(options.Enabled);
        Assert.Equal(0, options.Timeout);
        Assert.Null(options.Score);
    }

    /// <summary>Presence is the only question asked here; an invalid value is still the converter's failure, thrown while binding.</summary>
    [Fact]
    public void PresentButInvalidValue_StillThrowsTheInvariantFailure()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Bind<ValueTypeOptions>("""{ "Retry": { "MaxRetries": -5 } }"""));

        // Assert.Throws is exact-type, so reaching here already rules out a validation failure.
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    // ── Named options ───────────────────────────────────────────────────

    [Fact]
    public void NamedOptions_ValidateOnlyTheirOwnName()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>("configured").BindStrongTypes(Section(FullyConfigured));
        services.AddOptions<RetryOptions>("incomplete").BindStrongTypes(Section("""{ "Retry": { } }"""));

        var monitor = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<RetryOptions>>();

        Assert.Equal("checkout", monitor.Get("configured").Name.Value);
        Assert.Throws<OptionsValidationException>(() => monitor.Get("incomplete"));
    }

    // ── Nullable reference types disabled ───────────────────────────────

    /// <summary>An options class from an assembly compiled without nullable reference types declares no intent, so nothing is enforced rather than guessed at.</summary>
    [Fact]
    public void UnannotatedAssembly_IsNotPoliced()
    {
        var options = Bind<UnannotatedOptions>("""{ "Retry": { } }""");

        Assert.Null(options.Name);
    }

    // ── Depth ───────────────────────────────────────────────────────────

    private sealed class NestedRoot
    {
        public Level Child { get; set; } = null!;
    }

    private sealed class OptionalNestedRoot
    {
        public Level? Child { get; set; }
    }

    private sealed class CollectionRoot
    {
        public List<Level> Items { get; set; } = null!;
    }

    private sealed class DictionaryRoot
    {
        public Dictionary<string, Level> Map { get; set; } = null!;
    }

    private sealed class Level
    {
        public NonEmptyString Wrapper { get; set; } = null!;
        public string Plain { get; set; } = null!;
        public string? Optional { get; set; }
        public Positive<int> Number { get; set; }
    }

    [Fact]
    public void NestedObject_HasItsOwnPropertiesChecked()
    {
        var failures = Failures(BindExpectingFailure<NestedRoot>("""{ "Retry": { "Child": { "Plain": "x" } } }"""));

        Assert.Contains("'Retry:Child:Wrapper' is null", failures, StringComparison.Ordinal);
        Assert.Contains("Level.Wrapper", failures, StringComparison.Ordinal);
    }

    /// <summary>Also pins that the walk stops at a wrapper — it is not recursed into — and that a nested value type is still never required.</summary>
    [Fact]
    public void NestedObject_FullyConfigured_Passes()
    {
        var child = Bind<NestedRoot>("""{ "Retry": { "Child": { "Wrapper": "w", "Plain": "x" } } }""").Child;

        Assert.Equal("w", child.Wrapper.Value);
        Assert.Null(child.Optional);
        Assert.Equal(1, child.Number.Value);
    }

    [Fact]
    public void ConfiguredOptionalNested_StillHasItsOwnPropertiesChecked() =>
        Assert.Contains(
            "'Retry:Child:Wrapper' is null",
            Failures(BindExpectingFailure<OptionalNestedRoot>("""{ "Retry": { "Child": { "Plain": "x" } } }""")),
            StringComparison.Ordinal);

    [Fact]
    public void AbsentOptionalNested_IsNotChecked() =>
        Assert.Null(Bind<OptionalNestedRoot>("""{ "Retry": { } }""").Child);

    [Fact]
    public void CollectionElements_HaveTheirPropertiesChecked()
    {
        var failures = Failures(BindExpectingFailure<CollectionRoot>(
            """{ "Retry": { "Items": [ { "Wrapper": "ok", "Plain": "x" }, { "Plain": "y" } ] } }"""));

        Assert.Contains("'Retry:Items:1:Wrapper' is null", failures, StringComparison.Ordinal);
        Assert.DoesNotContain("Items:0", failures, StringComparison.Ordinal);
    }

    [Fact]
    public void DictionaryValues_HaveTheirPropertiesChecked() =>
        Assert.Contains(
            "'Retry:Map:primary:Wrapper' is null",
            Failures(BindExpectingFailure<DictionaryRoot>("""{ "Retry": { "Map": { "primary": { "Plain": "x" } } } }""")),
            StringComparison.Ordinal);

    private sealed class CycleRoot
    {
        public SelfReferencing Node { get; set; } = null!;
    }

    /// <summary>Binding cannot build a cycle, but a constructor can hand one to the walk.</summary>
    private sealed class SelfReferencing
    {
        public SelfReferencing() => Self = this;

        public SelfReferencing Self { get; set; }
        public string Name { get; set; } = null!;
        public NonEmptyString Wrapper { get; set; } = null!;
    }

    [Fact]
    public void SelfReferencingGraph_TerminatesAndReportsOnce() =>
        Assert.Equal(
            "'Retry:Node:Wrapper' is null. Configure it, give SelfReferencing.Wrapper a default, or declare it nullable.",
            Assert.Single(BindExpectingFailure<CycleRoot>("""{ "Retry": { "Node": { "Name": "n" } } }""").Failures));

    // ── Guard rails ─────────────────────────────────────────────────────

    [Fact]
    public void NullSection_Throws()
    {
        var builder = new ServiceCollection().AddOptions<RetryOptions>();

        Assert.Throws<ArgumentNullException>(() => builder.BindStrongTypes(null!));
    }
}
