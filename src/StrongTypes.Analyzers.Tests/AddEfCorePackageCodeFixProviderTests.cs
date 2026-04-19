using System.Xml.Linq;
using StrongTypes.Analyzers.Tests.Infrastructure;
using Xunit;

namespace StrongTypes.Analyzers.Tests;

/// <summary>
/// Behaviour tests for <see cref="AddEfCorePackageCodeFixProvider"/>. The fix operates on a csproj
/// file, not a Roslyn document, so tests materialize a temp directory, point an
/// <see cref="Microsoft.CodeAnalysis.AdhocWorkspace"/> at it, run the fix, and inspect the XML
/// on disk afterwards.
/// </summary>
public class AddEfCorePackageCodeFixProviderTests : IDisposable
{
    private readonly string _tempDir;

    public AddEfCorePackageCodeFixProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "StrongTypes.Analyzers.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; some environments hold file handles transiently on CI.
        }
    }

    [Fact]
    public async Task Adds_package_reference_into_existing_item_group()
    {
        var csproj = WriteCsproj("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """);

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddEfCorePackageCodeFixProvider(), csproj);
        var fix = Assert.Single(fixes);
        await CodeFixTester.ApplyAsync(fix);

        var doc = XDocument.Load(csproj);
        var packageReferences = doc.Descendants("PackageReference").ToArray();

        Assert.Equal(2, packageReferences.Length);
        Assert.Contains(packageReferences, p =>
            (string?)p.Attribute("Include") == MissingEfCorePackageAnalyzer.EfCorePackageId);

        // New reference should have been placed inside the existing ItemGroup (not a new one).
        var itemGroups = doc.Descendants("ItemGroup").ToArray();
        Assert.Single(itemGroups);
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

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddEfCorePackageCodeFixProvider(), csproj);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        var doc = XDocument.Load(csproj);
        var packageReferences = doc.Descendants("PackageReference").ToArray();
        var newRef = Assert.Single(packageReferences);

        Assert.Equal(MissingEfCorePackageAnalyzer.EfCorePackageId, (string?)newRef.Attribute("Include"));
        Assert.False(string.IsNullOrEmpty((string?)newRef.Attribute("Version")));
        Assert.Equal("ItemGroup", newRef.Parent!.Name.LocalName);
    }

    [Fact]
    public async Task Idempotent_when_package_reference_already_exists()
    {
        var csproj = WriteCsproj($$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{MissingEfCorePackageAnalyzer.EfCorePackageId}}" Version="0.3.0" />
              </ItemGroup>
            </Project>
            """);
        var before = File.ReadAllText(csproj);

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddEfCorePackageCodeFixProvider(), csproj);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        // No duplicate entry, and the file is byte-for-byte unchanged.
        var doc = XDocument.Load(csproj);
        Assert.Single(doc.Descendants("PackageReference"));
        Assert.Equal(before, File.ReadAllText(csproj));
    }

    [Fact]
    public async Task Match_on_package_id_is_case_insensitive()
    {
        var csproj = WriteCsproj($$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{MissingEfCorePackageAnalyzer.EfCorePackageId.ToLowerInvariant()}}" Version="0.3.0" />
              </ItemGroup>
            </Project>
            """);

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddEfCorePackageCodeFixProvider(), csproj);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        var doc = XDocument.Load(csproj);
        Assert.Single(doc.Descendants("PackageReference"));
    }

    [Fact]
    public async Task NoOp_when_csproj_file_is_missing()
    {
        // Point at a path that doesn't exist on disk. The fix must still register (it has no way
        // to check disk state during registration) but running it should not create the file or
        // throw.
        var csproj = Path.Combine(_tempDir, "Missing.csproj");

        var fixes = await CodeFixTester.RegisterFixesAsync(new AddEfCorePackageCodeFixProvider(), csproj);
        await CodeFixTester.ApplyAsync(Assert.Single(fixes));

        Assert.False(File.Exists(csproj));
    }

    [Fact]
    public async Task Advertises_the_expected_diagnostic_id_and_fix_all_provider()
    {
        var provider = new AddEfCorePackageCodeFixProvider();

        Assert.Contains(MissingEfCorePackageAnalyzer.DiagnosticId, provider.FixableDiagnosticIds);
        Assert.NotNull(provider.GetFixAllProvider());

        // Sanity-check the action's title so refactors of the message catch this test.
        var csproj = WriteCsproj("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        var fixes = await CodeFixTester.RegisterFixesAsync(provider, csproj);
        Assert.Contains(MissingEfCorePackageAnalyzer.EfCorePackageId, Assert.Single(fixes).Title);
    }

    private string WriteCsproj(string xml)
    {
        var path = Path.Combine(_tempDir, "Target.csproj");
        File.WriteAllText(path, xml);
        return path;
    }
}
