using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StrongTypes.Analyzers;

/// <summary>
/// Code fix for <see cref="MissingOpenApiPackageAnalyzer"/>: appends the
/// matching <c>&lt;PackageReference Include="Kalicz.StrongTypes.OpenApi.*" …/&gt;</c>
/// to the project's csproj. Which package depends on the diagnostic id —
/// ST0002 → <c>Kalicz.StrongTypes.OpenApi.Microsoft</c>,
/// ST0003 → <c>Kalicz.StrongTypes.OpenApi.Swashbuckle</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddOpenApiPackageCodeFixProvider))]
[Shared]
public sealed class AddOpenApiPackageCodeFixProvider : CodeFixProvider
{
    public const string OpenApiPackageVersion = "0.3.0";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId,
            MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var project = context.Document.Project;
        if (string.IsNullOrEmpty(project.FilePath)) return Task.CompletedTask;

        foreach (var diagnostic in context.Diagnostics)
        {
            var packageId = ResolvePackageId(diagnostic.Id);
            if (packageId is null) continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add PackageReference to {packageId}",
                    createChangedSolution: ct => AddPackageReferenceAsync(project, packageId, ct),
                    equivalenceKey: $"{nameof(AddOpenApiPackageCodeFixProvider)}:{packageId}"),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static string? ResolvePackageId(string diagnosticId) => diagnosticId switch
    {
        MissingOpenApiPackageAnalyzer.MicrosoftDiagnosticId => MissingOpenApiPackageAnalyzer.MicrosoftAdapterPackageId,
        MissingOpenApiPackageAnalyzer.SwashbuckleDiagnosticId => MissingOpenApiPackageAnalyzer.SwashbuckleAdapterPackageId,
        _ => null,
    };

#pragma warning disable RS1035 // File IO is intentional: PackageReference lives in the csproj, not in source documents.
    private static Task<Solution> AddPackageReferenceAsync(Project project, string packageId, CancellationToken cancellationToken)
    {
        var csprojPath = project.FilePath!;
        if (!File.Exists(csprojPath)) return Task.FromResult(project.Solution);

        var doc = XDocument.Load(csprojPath);
        if (doc.Root is null) return Task.FromResult(project.Solution);

        var existing = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .FirstOrDefault(e => string.Equals(
                (string?)e.Attribute("Include"),
                packageId,
                System.StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return Task.FromResult(project.Solution);

        var targetItemGroup = doc.Descendants()
            .Where(e => e.Name.LocalName == "ItemGroup"
                        && e.Elements().Any(c => c.Name.LocalName == "PackageReference"))
            .FirstOrDefault();

        var packageReference = new XElement(
            doc.Root.GetDefaultNamespace() + "PackageReference",
            new XAttribute("Include", packageId),
            new XAttribute("Version", OpenApiPackageVersion));

        if (targetItemGroup is not null)
            targetItemGroup.Add(packageReference);
        else
            doc.Root.Add(new XElement(doc.Root.GetDefaultNamespace() + "ItemGroup", packageReference));

        doc.Save(csprojPath);
        return Task.FromResult(project.Solution);
    }
#pragma warning restore RS1035
}
