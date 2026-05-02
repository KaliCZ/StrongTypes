using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

/// <summary>
/// Behaviour tests for <see cref="MissingOpenApiPackageAnalyzer"/> (ST0002 / ST0003). Each test
/// assembles a minimal source that should or should not trip the analyzer, drives it through the
/// real Roslyn pipeline, and asserts the resulting diagnostics.
/// </summary>
public class MissingOpenApiPackageAnalyzerTests
{
    private const string DtoWithNonEmptyStringProperty = """
        using StrongTypes;

        namespace Sample;

        public class CreateUserRequest
        {
            public NonEmptyString Name { get; set; } = null!;
        }
        """;

    [Fact]
    public async Task Microsoft_fires_when_microsoft_openapi_referenced_without_strong_types_adapter()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi));

        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Fact]
    public async Task Microsoft_silent_when_strong_types_adapter_is_referenced()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi, TestReferences.StrongTypesOpenApiMicrosoft));

        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
    }

    [Fact]
    public async Task Swashbuckle_fires_when_swashbuckle_referenced_without_strong_types_adapter()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With(TestReferences.SwashbuckleAspNetCoreSwaggerGen));

        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
    }

    [Fact]
    public async Task Swashbuckle_silent_when_strong_types_adapter_is_referenced()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With(TestReferences.SwashbuckleAspNetCoreSwaggerGen, TestReferences.StrongTypesOpenApiSwashbuckle));

        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Fact]
    public async Task Silent_when_no_openapi_generator_is_referenced()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With());

        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Fact]
    public async Task Silent_when_no_strong_type_properties_exist()
    {
        const string source = """
            namespace Sample;

            public class CreateUserRequest
            {
                public string Name { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi, TestReferences.SwashbuckleAspNetCoreSwaggerGen));

        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Theory]
    [InlineData("NonEmptyString")]
    [InlineData("Positive<int>")]
    [InlineData("NonNegative<long>")]
    [InlineData("Negative<decimal>")]
    [InlineData("NonPositive<short>")]
    [InlineData("NonEmptyEnumerable<NonEmptyString>")]
    [InlineData("INonEmptyEnumerable<int>")]
    [InlineData("Maybe<NonEmptyString>")]
    public async Task Detects_each_strong_type_wrapper(string wrapperType)
    {
        var source = $$"""
            using StrongTypes;

            namespace Sample;

            public class Dto
            {
                public {{wrapperType}} Value { get; set; } = default!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi));

        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
    }

    [Fact]
    public async Task Detects_nullable_value_type_wrapper()
    {
        const string source = """
            using StrongTypes;

            namespace Sample;

            public class Dto
            {
                public Positive<int>? Quantity { get; set; }
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.SwashbuckleAspNetCoreSwaggerGen));

        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Fact]
    public async Task Fires_both_ids_when_both_generators_are_referenced_without_adapters()
    {
        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            DtoWithNonEmptyStringProperty,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi, TestReferences.SwashbuckleAspNetCoreSwaggerGen));

        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
        Assert.NotEmpty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId));
    }

    [Fact]
    public async Task Silent_on_internal_property()
    {
        const string source = """
            using StrongTypes;

            namespace Sample;

            public class Dto
            {
                internal NonEmptyString Hidden { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerTester.RunAsync(
            new MissingOpenApiPackageAnalyzer(),
            source,
            TestReferences.With(TestReferences.MicrosoftAspNetCoreOpenApi));

        Assert.Empty(diagnostics.WhereId(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId));
    }
}
