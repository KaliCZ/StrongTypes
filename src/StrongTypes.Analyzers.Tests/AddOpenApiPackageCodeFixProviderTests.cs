using System.Xml.Linq;
using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

/// <summary>
/// Behaviour tests for <see cref="AddOpenApiPackageCodeFixProvider"/>. Mirrors the
/// EfCore code-fix tests — point an <see cref="Microsoft.CodeAnalysis.AdhocWorkspace"/>
/// at a temp csproj, run the fix, and assert the XML on disk afterwards. Both diagnostic
/// IDs (ST0002 / ST0003) are exercised so each picks the right package.
/// </summary>
public class AddOpenApiPackageCodeFixProviderTests : IDisposable
{
    private readonly string _tempDir;

    public AddOpenApiPackageCodeFixProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "StrongTypes.Analyzers.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; some environments hold file handles transiently on CI.
        }
    }

    [Theory]
    [InlineData(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId, MissingOpenApiPackageAnalyzer.MicrosoftAdapterPackageId)]
    [InlineData(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId, MissingOpenApiPackageAnalyzer.SwashbuckleAdapterPackageId)]
    public async Task Adds_matching_adapter_package_for_diagnostic_id(string diagnosticId, string expectedPackageId)
    {
        var csproj = WriteCsproj("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Some.Package" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddOpenApiPackageCodeFixProvider(), csproj, diagnosticId);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        var doc = XDocument.Load(csproj);
        var packageReferences = doc.Descendants("PackageReference").ToArray();

        Assert.Equal(2, packageReferences.Length);
        Assert.Contains(packageReferences, p => (string?)p.Attribute("Include") == expectedPackageId);
        Assert.Single(doc.Descendants("ItemGroup"));
    }

    [Fact]
    public async Task Creates_new_item_group_when_none_contains_package_references()
    {
        var csproj = WriteCsproj("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var fixes = await CodeFixTester.RegisterFixesAsync(
            new AddOpenApiPackageCodeFixProvider(),
            csproj,
            MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        var doc = XDocument.Load(csproj);
        var newRef = Assert.Single(doc.Descendants("PackageReference"));

        Assert.Equal(MissingOpenApiPackageAnalyzer.MicrosoftAdapterPackageId, (string?)newRef.Attribute("Include"));
        Assert.Equal(AddOpenApiPackageCodeFixProvider.OpenApiPackageVersion, (string?)newRef.Attribute("Version"));
        Assert.Single(doc.Descendants("ItemGroup"));
    }

    [Fact]
    public async Task Idempotent_when_adapter_package_reference_already_exists()
    {
        var csproj = WriteCsproj($$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{MissingOpenApiPackageAnalyzer.SwashbuckleAdapterPackageId}}" Version="0.3.0" />
              </ItemGroup>
            </Project>
            """);
        var before = File.ReadAllText(csproj);

        var fixes = await CodeFixTester.RegisterFixesAsync(
            new AddOpenApiPackageCodeFixProvider(),
            csproj,
            MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        var doc = XDocument.Load(csproj);
        Assert.Single(doc.Descendants("PackageReference"));
        Assert.Equal(before, File.ReadAllText(csproj));
    }

    [Fact]
    public async Task Advertises_both_diagnostic_ids_and_fix_all_provider()
    {
        var provider = new AddOpenApiPackageCodeFixProvider();

        Assert.Contains(MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId, provider.FixableDiagnosticIds);
        Assert.Contains(MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId, provider.FixableDiagnosticIds);
        Assert.NotNull(provider.GetFixAllProvider());
    }

    private string WriteCsproj(string xml)
    {
        var path = Path.Combine(_tempDir, "Target.csproj");
        File.WriteAllText(path, xml);
        return path;
    }
}
