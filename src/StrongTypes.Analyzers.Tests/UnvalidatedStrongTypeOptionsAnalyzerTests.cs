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

    /// <summary>A non-nullable reference wrapper is the one an absent key leaves null — the state its type forbids.</summary>
    [Theory]
    [InlineData(BindRegistration)]
    [InlineData(ConfigureRegistration)]
    public async Task Fires_for_a_non_nullable_reference_wrapper(string registration)
    {
        var diagnostics = await RunAsync(Source("    public NonEmptyString Name { get; set; } = null!;", registration));

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("RetryOptions", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Name", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_every_unguarded_property_in_one_diagnostic()
    {
        var diagnostics = await RunAsync(Source("""
                public NonEmptyString Name { get; set; } = null!;
                public Email Contact { get; set; } = null!;
            """, BindRegistration));

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Name", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Contact", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    /// <summary>A wrapper nested in a child options class is left null by the same absent key, so the nudge has to reach it.</summary>
    [Fact]
    public async Task Fires_for_a_wrapper_nested_in_a_child_options_class()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            public class RetryOptions
            {
                public BackoffOptions Backoff { get; set; } = null!;
            }

            public class BackoffOptions
            {
                public NonEmptyString Name { get; set; } = null!;
            }

            public static class Startup
            {
                public static void Register(IServiceCollection services, IConfiguration config)
                {
                    services.AddOptions<RetryOptions>().Bind(config.GetSection("Retry"));
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Backoff.Name", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    /// <summary>The binder walks collection elements, so a wrapper inside one is left null by the same absent key.</summary>
    [Fact]
    public async Task Fires_for_a_wrapper_inside_a_collection_element()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            using System.Collections.Generic;

            public class RetryOptions
            {
                public List<EndpointOptions> Endpoints { get; set; } = null!;
            }

            public class EndpointOptions
            {
                public NonEmptyString Host { get; set; } = null!;
            }

            public static class Startup
            {
                public static void Register(IServiceCollection services, IConfiguration config)
                {
                    services.AddOptions<RetryOptions>().Bind(config.GetSection("Retry"));
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Endpoints.Host", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    // ── Silent ──────────────────────────────────────────────────────────

    /// <summary>
    /// A type carrying its own <c>[TypeConverter]</c> binds from a scalar, so the binder never reaches
    /// the wrapper inside it and neither should the walk — recursing here would invent a diagnostic
    /// for a property configuration was never going to touch.
    /// </summary>
    [Fact]
    public async Task Silent_for_a_wrapper_inside_a_type_that_binds_from_a_scalar()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            using System.ComponentModel;

            [TypeConverter(typeof(EndpointConverter))]
            public class Endpoint
            {
                public NonEmptyString Host { get; set; } = null!;
            }

            public class EndpointConverter : TypeConverter { }

            public class RetryOptions
            {
                public Endpoint Primary { get; set; } = null!;
            }

            public static class Startup
            {
                public static void Register(IServiceCollection services, IConfiguration config)
                {
                    services.AddOptions<RetryOptions>().Bind(config.GetSection("Retry"));
                }
            }
            """);

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>A nested class whose own wrappers are all guarded gives the walk nothing to report.</summary>
    [Fact]
    public async Task Silent_for_a_nested_class_with_nothing_unguarded()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            public class RetryOptions
            {
                public BackoffOptions Backoff { get; set; } = null!;
            }

            public class BackoffOptions
            {
                public NonEmptyString? Name { get; set; }
                public Positive<int> Attempts { get; set; }
            }

            public static class Startup
            {
                public static void Register(IServiceCollection services, IConfiguration config)
                {
                    services.AddOptions<RetryOptions>().Bind(config.GetSection("Retry"));
                }
            }
            """);

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>A self-referencing options class must not send the walk round forever.</summary>
    [Fact]
    public async Task Cyclic_options_graph_terminates()
    {
        var diagnostics = await RunAsync($$"""
            {{Header}}
            public class RetryOptions
            {
                public NodeOptions Head { get; set; } = null!;
            }

            public class NodeOptions
            {
                public NodeOptions? Next { get; set; }
                public NonEmptyString Name { get; set; } = null!;
            }

            public static class Startup
            {
                public static void Register(IServiceCollection services, IConfiguration config)
                {
                    services.AddOptions<RetryOptions>().Bind(config.GetSection("Retry"));
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
        Assert.Contains("Head.Name", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    /// <summary><c>[Required]</c> does find a null reference wrapper, so nagging there would be wrong.</summary>
    [Fact]
    public async Task Silent_for_a_reference_wrapper_carrying_Required()
    {
        var diagnostics = await RunAsync(Source(
            "    [Required] public NonEmptyString Name { get; set; } = null!;",
            BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Silent_when_every_wrapper_is_nullable()
    {
        var diagnostics = await RunAsync(Source("    public NonEmptyString? Name { get; set; }", BindRegistration));

        Assert.Empty(diagnostics.WhereId(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId));
    }

    /// <summary>
    /// A struct wrapper cannot be null, so an absent key leaves it at a default the type is happy to
    /// hold — <c>Positive&lt;int&gt;</c> at 1. There is no contradiction to report, and requiring
    /// configuration for it would be a policy rather than a fix.
    /// </summary>
    [Theory]
    [InlineData("    public Positive<int> MaxRetries { get; set; }")]
    [InlineData("    public Positive<int>? MaxRetries { get; set; }")]
    [InlineData("    public Digit Tier { get; set; }")]
    public async Task Silent_for_struct_wrappers(string options)
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
