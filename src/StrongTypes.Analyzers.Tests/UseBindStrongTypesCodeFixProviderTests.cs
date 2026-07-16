using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

public class UseBindStrongTypesCodeFixProviderTests
{
    private const string BindSource = """
        #nullable enable
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using StrongTypes;

        namespace Sample;

        public class RetryOptions
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
        """;

    private static IEnumerable<Microsoft.CodeAnalysis.MetadataReference> WithConfiguration() =>
        TestReferences.With([.. TestReferences.OptionsStack, TestReferences.StrongTypesConfiguration]);

    [Fact]
    public async Task Rewrites_Bind_to_BindStrongTypes()
    {
        var fixedSource = await DocumentCodeFixTester.ApplySingleFixAsync(
            new UnvalidatedStrongTypeOptionsAnalyzer(),
            new UseBindStrongTypesCodeFixProvider(),
            BindSource,
            WithConfiguration());

        Assert.Contains("services.AddOptions<RetryOptions>().BindStrongTypes(config.GetSection(\"Retry\"));", fixedSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".Bind(config", fixedSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adds_the_configuration_using()
    {
        var fixedSource = await DocumentCodeFixTester.ApplySingleFixAsync(
            new UnvalidatedStrongTypeOptionsAnalyzer(),
            new UseBindStrongTypesCodeFixProvider(),
            BindSource,
            WithConfiguration());

        Assert.Contains("using StrongTypes.Configuration;", fixedSource, StringComparison.Ordinal);
    }

    /// <summary>Offered even without the package — the rewrite won't compile until it is added, and the fix title names it.</summary>
    [Fact]
    public async Task Offered_without_the_package_naming_it_in_the_title()
    {
        var actions = await DocumentCodeFixTester.RegisterFixesAsync(
            new UnvalidatedStrongTypeOptionsAnalyzer(),
            new UseBindStrongTypesCodeFixProvider(),
            BindSource,
            TestReferences.With(TestReferences.OptionsStack));

        var action = Assert.Single(actions);
        Assert.Contains("Kalicz.StrongTypes.Configuration", action.Title, StringComparison.Ordinal);
    }

    /// <summary>Rewriting <c>Configure&lt;T&gt;</c> means restructuring to <c>AddOptions&lt;T&gt;().BindStrongTypes(…)</c>; the diagnostic still fires, but the change is the caller's to make.</summary>
    [Fact]
    public async Task Not_offered_for_Configure()
    {
        var actions = await DocumentCodeFixTester.RegisterFixesAsync(
            new UnvalidatedStrongTypeOptionsAnalyzer(),
            new UseBindStrongTypesCodeFixProvider(),
            BindSource.Replace(
                "services.AddOptions<RetryOptions>().Bind(config.GetSection(\"Retry\"));",
                "services.Configure<RetryOptions>(config.GetSection(\"Retry\"));",
                StringComparison.Ordinal),
            WithConfiguration());

        Assert.Empty(actions);
    }
}
