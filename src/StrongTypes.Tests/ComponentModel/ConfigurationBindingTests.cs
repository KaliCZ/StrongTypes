using System;
using System.IO;
using System.Text;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>
/// Without a <c>TypeConverter</c>, <c>ConfigurationBinder</c> silently leaves the property at its
/// default rather than failing — so these assert the bound value, not merely that binding returned.
/// </summary>
public class ConfigurationBindingTests
{
    public enum BindingPath
    {
        ConfigurationGet,
        Options,
    }

    public static TheoryData<BindingPath> Paths => new() { BindingPath.ConfigurationGet, BindingPath.Options };

    private sealed class RetryOptions
    {
        public NonEmptyString Name { get; set; } = NonEmptyString.Create("initial");
        public NonEmptyString? Nickname { get; set; }
        public Positive<int> MaxRetries { get; set; }
        public Positive<int>? Score { get; set; }
        public NonNegative<int> InitialDelaySeconds { get; set; }
        public Email? Contact { get; set; }
        public Digit Tier { get; set; }
    }

    private static IConfiguration Config(string settings) =>
        new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes("{\"Retry\":{" + settings + "}}")))
            .Build();

    private static RetryOptions Bind(BindingPath path, string settings)
    {
        var section = Config(settings).GetSection("Retry");
        if (path is BindingPath.ConfigurationGet)
            return section.Get<RetryOptions>() ?? new RetryOptions();

        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(section);
        return services.BuildServiceProvider().GetRequiredService<IOptions<RetryOptions>>().Value;
    }

    // ── The scenario the converter unlocks ──────────────────────────────

    [Theory]
    [MemberData(nameof(Paths))]
    public void EveryStrongTypeBindsFromConfiguration(BindingPath path)
    {
        var options = Bind(path, """
            "Name": "checkout",
            "Nickname": "co",
            "MaxRetries": 5,
            "Score": 99,
            "InitialDelaySeconds": 0,
            "Contact": "ops@example.com",
            "Tier": 7
            """);

        Assert.Equal("checkout", options.Name.Value);
        Assert.Equal("co", options.Nickname?.Value);
        Assert.Equal(5, options.MaxRetries.Value);
        Assert.Equal(99, options.Score?.Value);
        Assert.Equal(0, options.InitialDelaySeconds.Value);
        Assert.Equal("ops@example.com", options.Contact?.Address);
        Assert.Equal(7, options.Tier.Value);
    }

    /// <summary><c>default(Positive&lt;int&gt;)</c> is a plausible-looking <c>1</c>, so a dropped config value reads as a real one — hence asserting the bound value differs from the default.</summary>
    [Property]
    public void BoundValueComesFromConfiguration_NotTheDefault(int configured)
    {
        if (configured <= 0 || configured == default(Positive<int>).Value) return;

        foreach (var path in new[] { BindingPath.ConfigurationGet, BindingPath.Options })
        {
            var options = Bind(path, $"\"MaxRetries\": {configured}");

            Assert.Equal(configured, options.MaxRetries.Value);
            Assert.NotEqual(default(Positive<int>).Value, options.MaxRetries.Value);
        }
    }

    // ── Absent vs. explicit null ────────────────────────────────────────

    /// <summary>An absent key leaves the property exactly as the options class constructed it — here a non-null initialiser.</summary>
    [Theory]
    [MemberData(nameof(Paths))]
    public void AbsentKey_LeavesWhateverThePropertyAlreadyHeld(BindingPath path)
    {
        var options = Bind(path, "\"Tier\": 7");

        Assert.Equal("initial", options.Name.Value);
        Assert.Equal(default(Positive<int>).Value, options.MaxRetries.Value);
        Assert.Null(options.Nickname);
        Assert.Null(options.Score);
    }

    /// <summary>An explicit <c>null</c> is not the same as an absent key: it overwrites, so a struct wrapper falls back to <c>default</c> rather than keeping the value the object was constructed with.</summary>
    [Theory]
    [MemberData(nameof(Paths))]
    public void ExplicitNull_OnAStructWrapper_LeavesTheDefault(BindingPath path)
    {
        var options = Bind(path, "\"MaxRetries\": null, \"Score\": null");

        Assert.Equal(default(Positive<int>).Value, options.MaxRetries.Value);
        Assert.Null(options.Score);
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void ExplicitNull_OnANullableReferenceWrapper_IsNull(BindingPath path) =>
        Assert.Null(Bind(path, "\"Nickname\": null").Nickname);

    /// <summary>A <c>null</c> in config nulls even a non-nullable reference property, because nullability is erased by the time the binder runs — <c>NonEmptyString</c> is no different from <c>string</c> here.</summary>
    [Theory]
    [MemberData(nameof(Paths))]
    public void ExplicitNull_DefeatsANonNullableReferenceWrapper(BindingPath path) =>
        Assert.Null(Bind(path, "\"Name\": null").Name);

    // ── Empty string: not uniform, and not obvious ──────────────────────

    /// <summary>Empty is not a legal <see cref="NonEmptyString"/>, and being declared nullable does not make it one — our converter is handed <c>""</c> either way, because a nullable reference is the same runtime type.</summary>
    [Theory]
    [InlineData(BindingPath.ConfigurationGet, "\"Name\": \"\"")]
    [InlineData(BindingPath.ConfigurationGet, "\"Name\": \"  \"")]
    [InlineData(BindingPath.ConfigurationGet, "\"Nickname\": \"\"")]
    [InlineData(BindingPath.ConfigurationGet, "\"Nickname\": \"  \"")]
    [InlineData(BindingPath.Options, "\"Name\": \"\"")]
    [InlineData(BindingPath.Options, "\"Name\": \"  \"")]
    [InlineData(BindingPath.Options, "\"Nickname\": \"\"")]
    [InlineData(BindingPath.Options, "\"Nickname\": \"  \"")]
    public void EmptyString_OnAReferenceWrapper_Throws_NullableOrNot(BindingPath path, string settings) =>
        Assert.Throws<InvalidOperationException>(() => Bind(path, settings));

    [Theory]
    [MemberData(nameof(Paths))]
    public void EmptyString_OnANonNullableStructWrapper_Throws(BindingPath path) =>
        Assert.Throws<InvalidOperationException>(() => Bind(path, "\"MaxRetries\": \"\""));

    /// <summary>The one asymmetry: empty binds to <c>null</c> here rather than throwing.</summary>
    [Theory]
    [MemberData(nameof(Paths))]
    public void EmptyString_OnANullableStructWrapper_IsNull(BindingPath path) =>
        Assert.Null(Bind(path, "\"Score\": \"\"").Score);

    // ── Invalid configuration fails, and says why ───────────────────────

    [Theory]
    [InlineData(BindingPath.ConfigurationGet, "\"MaxRetries\": -5", "MaxRetries", "-5", "must be positive")]
    [InlineData(BindingPath.ConfigurationGet, "\"MaxRetries\": 0", "MaxRetries", "0", "must be positive")]
    [InlineData(BindingPath.ConfigurationGet, "\"Score\": -1", "Score", "-1", "must be positive")]
    [InlineData(BindingPath.ConfigurationGet, "\"InitialDelaySeconds\": -1", "InitialDelaySeconds", "-1", "must be non-negative")]
    [InlineData(BindingPath.ConfigurationGet, "\"Contact\": \"not-an-email\"", "Contact", "not-an-email", "valid email")]
    [InlineData(BindingPath.ConfigurationGet, "\"Tier\": 42", "Tier", "42", "single decimal digit")]
    [InlineData(BindingPath.Options, "\"MaxRetries\": -5", "MaxRetries", "-5", "must be positive")]
    [InlineData(BindingPath.Options, "\"MaxRetries\": 0", "MaxRetries", "0", "must be positive")]
    [InlineData(BindingPath.Options, "\"Score\": -1", "Score", "-1", "must be positive")]
    [InlineData(BindingPath.Options, "\"InitialDelaySeconds\": -1", "InitialDelaySeconds", "-1", "must be non-negative")]
    [InlineData(BindingPath.Options, "\"Contact\": \"not-an-email\"", "Contact", "not-an-email", "valid email")]
    [InlineData(BindingPath.Options, "\"Tier\": 42", "Tier", "42", "single decimal digit")]
    public void ValueBreakingTheInvariant_ThrowsAndNamesTheReason(
        BindingPath path, string settings, string key, string value, string expectedReason)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Bind(path, settings));

        Assert.Contains($"Retry:{key}", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);

        var inner = Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Contains(expectedReason, inner.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(BindingPath.ConfigurationGet, "\"MaxRetries\": \"not-a-number\"")]
    [InlineData(BindingPath.ConfigurationGet, "\"Tier\": \"not-a-digit\"")]
    [InlineData(BindingPath.Options, "\"MaxRetries\": \"not-a-number\"")]
    [InlineData(BindingPath.Options, "\"Tier\": \"not-a-digit\"")]
    public void ValueInTheWrongFormat_Throws(BindingPath path, string settings) =>
        Assert.Throws<InvalidOperationException>(() => Bind(path, settings));

    // ── When the failure surfaces ───────────────────────────────────────

    /// <summary>Binding is lazy: the failure surfaces on first <c>IOptions.Value</c>, not at <c>BuildServiceProvider</c>.</summary>
    [Fact]
    public void InvalidValue_DoesNotThrowUntilOptionsAreRead()
    {
        var services = new ServiceCollection();
        services.AddOptions<RetryOptions>().Bind(Config("\"MaxRetries\": -5").GetSection("Retry"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IOptions<RetryOptions>>();

        Assert.Throws<InvalidOperationException>(() => accessor.Value);
    }
}
