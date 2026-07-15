using Microsoft.CodeAnalysis;
using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

/// <summary>
/// Behaviour tests for <see cref="UnvalidatedStrongTypeOptionsAnalyzer"/> (ST0004). Each source
/// opts into nullable reference types, because required-ness for a reference wrapper is read from
/// the annotation and the test compilation does not enable it project-wide.
/// </summary>
public class UnvalidatedStrongTypeOptionsAnalyzerTests
{
    private const string Header = """
        #nullable enable
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using System.ComponentModel.DataAnnotations;
        using StrongTypes;

        namespace Sample;

        """;

    private static string Source(string options, string registration) => $$"""
        {{Header}}
        public class RetryOptions
        {
        {{options}}
        }

        public static class Startup
        {
            public static void Register(IServiceCollection services, IConfiguration config)
            {
        {{registration}}
            }
        }
        """;

    private const string BindRegistration = "        services.AddOptions<RetryOptions>().Bind(config.GetSection(\"Retry\"));";
    private const string ConfigureRegistration = "        services.Configure<RetryOptions>(config.GetSection(\"Retry\"));";

    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> RunAsync(string source) =>
        AnalyzerTester.RunAsync(new UnvalidatedStrongTypeOptionsAnalyzer(), source, TestReferences.With(TestReferences.OptionsStack));

    // ── Fires ───────────────────────────────────────────────────────────

    /// <summary>The case nothing else catches: <c>default(Positive&lt;int&gt;)</c> is 1, so <c>[Required]</c> would pass even if it were there.</summary>
    [Theory]
    [InlineData(BindRegistration)]
    [InlineData(ConfigureRegistration)]
    public async Task Fires_for_a_non_nullable_struct_wrapper(string registration)
    {
        var diagnostics = await RunAsync(Source("    public Positive<int> MaxRetries { get; set; }", registration));

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("RetryOptions", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("MaxRetries", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    /// <summary>A struct wrapper's default is never null, so <c>[Required]</c> cannot rescue it and the diagnostic still fires.</summary>
    [Fact]
    public async Task Fires_for_a_non_nullable_struct_wrapper_even_with_Required()
    {
        var diagnostics = await RunAsync(Source(
            "    [Required] public Positive<int> MaxRetries { get; set; }",
            BindRegistration));

        Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Fires_for_a_non_nullable_reference_wrapper_without_Required()
    {
        var diagnostics = await RunAsync(Source(
            "    public NonEmptyString Name { get; set; } = null!;",
            BindRegistration));

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Name", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_every_unguarded_property_in_one_diagnostic()
    {
        var diagnostics = await RunAsync(Source("""
                public NonEmptyString Name { get; set; } = null!;
                public Positive<int> MaxRetries { get; set; }
            """, BindRegistration));

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Name", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("MaxRetries", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    // ── Silent ──────────────────────────────────────────────────────────

    /// <summary><c>[Required]</c> does find a null reference wrapper, so nagging there would be wrong.</summary>
    [Fact]
    public async Task Silent_for_a_reference_wrapper_carrying_Required()
    {
        var diagnostics = await RunAsync(Source(
            "    [Required] public NonEmptyString Name { get; set; } = null!;",
            BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    [Theory]
    [InlineData("    public Positive<int>? MaxRetries { get; set; }")]
    [InlineData("    public NonEmptyString? Name { get; set; }")]
    public async Task Silent_when_every_wrapper_is_nullable(string options)
    {
        var diagnostics = await RunAsync(Source(options, BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Silent_when_the_options_type_holds_no_strong_types()
    {
        var diagnostics = await RunAsync(Source("""
                public string Name { get; set; } = "";
                public int MaxRetries { get; set; }
            """, BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>A read-only property is not something the binder assigns, so its absence is not a configuration problem.</summary>
    [Fact]
    public async Task Silent_for_a_get_only_property()
    {
        var diagnostics = await RunAsync(Source(
            "    public Positive<int> MaxRetries { get; } = Positive<int>.Create(3);",
            BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>Nothing to say when the project never binds options — the analyzer must no-op rather than fire on the declaration alone.</summary>
    [Fact]
    public async Task Silent_when_the_options_type_is_never_bound()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            public class RetryOptions
            {
                public Positive<int> MaxRetries { get; set; }
            }
            """);

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>Without the Options stack referenced there is no <c>Bind</c> to match, and the analyzer must stay silent rather than throw.</summary>
    [Fact]
    public async Task Silent_when_options_is_not_referenced()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new UnvalidatedStrongTypeOptionsAnalyzer(),
            """
            #nullable enable
            using StrongTypes;

            namespace Sample;

            public class RetryOptions
            {
                public Positive<int> MaxRetries { get; set; }
            }
            """,
            TestReferences.With());

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }
}
