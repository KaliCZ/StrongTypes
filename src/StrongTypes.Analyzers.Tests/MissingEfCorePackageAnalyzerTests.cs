using Microsoft.CodeAnalysis;
using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

/// <summary>
/// Behaviour tests for <see cref="MissingEfCorePackageAnalyzer"/> (ST0001). Each test assembles a
/// minimal source that either should or should not trip the analyzer, drives it through the real
/// Roslyn pipeline, and asserts the resulting diagnostics.
/// </summary>
public class MissingEfCorePackageAnalyzerTests
{
    private const string DbContextWithNonEmptyStringEntity = """
        using Microsoft.EntityFrameworkCore;
        using StrongTypes;

        namespace Sample;

        public class Product
        {
            public int Id { get; set; }
            public NonEmptyString Name { get; set; } = null!;
        }

        public class SampleContext : DbContext
        {
            public DbSet<Product> Products { get; set; } = null!;
        }
        """;

    [Fact]
    public async Task Fires_when_ef_core_referenced_without_strong_types_ef_core()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            DbContextWithNonEmptyStringEntity,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, d => Assert.Equal(MissingEfCorePackageAnalyzer.DiagnosticId, d.Id));
    }

    [Fact]
    public async Task Silent_when_strong_types_ef_core_is_referenced()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            DbContextWithNonEmptyStringEntity,
            TestReferences.With(TestReferences.EntityFrameworkCore, TestReferences.StrongTypesEfCore));

        Assert.Empty(diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Silent_when_ef_core_is_not_referenced()
    {
        // Without EF Core the compilation wouldn't even compile `DbContext` — use a plain class
        // so we're testing the early-return on reference scan, not a build failure.
        const string source = """
            using StrongTypes;

            namespace Sample;

            public class Product
            {
                public NonEmptyString Name { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            source,
            TestReferences.With());

        Assert.Empty(diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Silent_when_entity_has_no_strong_type_properties()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;

            namespace Sample;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; } = null!;
            }

            public class SampleContext : DbContext
            {
                public DbSet<Product> Products { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        Assert.Empty(diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Detects_nullable_value_type_wrapper()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;
            using StrongTypes;

            namespace Sample;

            public class Order
            {
                public int Id { get; set; }
                public Positive<int>? Quantity { get; set; }
            }

            public class SampleContext : DbContext
            {
                public DbSet<Order> Orders { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        Assert.NotEmpty(diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId));
    }

    [Theory]
    [InlineData("Positive<int>")]
    [InlineData("NonNegative<long>")]
    [InlineData("Negative<decimal>")]
    [InlineData("NonPositive<short>")]
    public async Task Detects_each_generic_numeric_wrapper(string wrapperType)
    {
        var source = $$"""
            using Microsoft.EntityFrameworkCore;
            using StrongTypes;

            namespace Sample;

            public class Entity
            {
                public int Id { get; set; }
                public {{wrapperType}} Value { get; set; }
            }

            public class SampleContext : DbContext
            {
                public DbSet<Entity> Entities { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        Assert.NotEmpty(diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId));
    }

    [Fact]
    public async Task Reports_at_dbset_entity_property_and_dbcontext_locations()
    {
        // Single DbContext + single DbSet + entity with one wrapper property should produce
        // exactly three diagnostics: one at the DbSet property, one at the entity property,
        // one at the DbContext class declaration.
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            DbContextWithNonEmptyStringEntity,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        var st0001 = diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId).ToArray();
        Assert.Equal(3, st0001.Length);

        var lines = st0001
            .Select(d => d.Location.GetLineSpan().StartLinePosition.Line)
            .OrderBy(l => l)
            .ToArray();
        Assert.Equal(3, lines.Distinct().Count());
    }

    [Fact]
    public async Task Reports_each_strong_type_property_on_the_entity()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;
            using StrongTypes;

            namespace Sample;

            public class Product
            {
                public int Id { get; set; }
                public NonEmptyString Name { get; set; } = null!;
                public Positive<int> Quantity { get; set; }
            }

            public class SampleContext : DbContext
            {
                public DbSet<Product> Products { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingEfCorePackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.EntityFrameworkCore));

        var st0001 = diagnostics.WhereId(MissingEfCorePackageAnalyzer.DiagnosticId).ToArray();
        // 1 on DbSet + 2 on the two wrapper properties + 1 on DbContext class = 4.
        Assert.Equal(4, st0001.Length);
    }
}

internal static class DiagnosticFilters
{
    public static IEnumerable<Diagnostic> WhereId(this IEnumerable<Diagnostic> diagnostics, string id) =>
        diagnostics.Where(d => d.Id == id);
}
