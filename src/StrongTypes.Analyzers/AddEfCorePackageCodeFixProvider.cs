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
/// Code fix for <see cref="MissingEfCorePackageAnalyzer"/>: appends
/// <c>&lt;PackageReference Include="Kalicz.StrongTypes.EfCore" …/&gt;</c>
/// to the project's csproj. Implemented as a file-system side effect rather
/// than a document edit because <c>PackageReference</c> lives in a csproj,
/// which Roslyn doesn't model as a first-class document.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddEfCorePackageCodeFixProvider))]
[Shared]
public sealed class AddEfCorePackageCodeFixProvider : CodeFixProvider
{
    // Kept in one place so bumping the EfCore version is a one-line change.
    private const string EfCorePackageVersion = "0.3.0";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(MissingEfCorePackageAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var project = context.Document.Project;
        if (string.IsNullOrEmpty(project.FilePath))
        {
            return Task.CompletedTask;
        }
        // RS1035 forbids file IO in analyzers; a code fix that installs a
        // NuGet package intrinsically has to touch the csproj, so existence
        // checking + XML round-trip happens inside the CodeAction delegate,
        // suppressed at the call sites below.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Add PackageReference to {MissingEfCorePackageAnalyzer.EfCorePackageId}",
                createChangedSolution: ct => AddPackageReferenceAsync(context.Document.Project, ct),
                equivalenceKey: nameof(AddEfCorePackageCodeFixProvider)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

#pragma warning disable RS1035 // File IO is intentional: PackageReference lives in the csproj, not in source documents.
    private static Task<Solution> AddPackageReferenceAsync(Project project, CancellationToken cancellationToken)
    {
        var csprojPath = project.FilePath!;
        if (!File.Exists(csprojPath))
        {
            return Task.FromResult(project.Solution);
        }
        var doc = XDocument.Load(csprojPath);
        if (doc.Root is null)
        {
            return Task.FromResult(project.Solution);
        }

        // Idempotency: do nothing if a PackageReference for the EfCore package
        // is already present (covers the edge case where the diagnostic fires
        // on a stale cache and the user has already installed manually).
        var existing = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .FirstOrDefault(e => string.Equals(
                (string?)e.Attribute("Include"),
                MissingEfCorePackageAnalyzer.EfCorePackageId,
                System.StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(project.Solution);
        }

        // Prefer to slot the new PackageReference into an existing ItemGroup
        // that already holds PackageReferences — matches how tooling like
        // `dotnet add package` shapes the file.
        var targetItemGroup = doc.Descendants()
            .Where(e => e.Name.LocalName == "ItemGroup"
                        && e.Elements().Any(c => c.Name.LocalName == "PackageReference"))
            .FirstOrDefault();

        var packageReference = new XElement(
            doc.Root.GetDefaultNamespace() + "PackageReference",
            new XAttribute("Include", MissingEfCorePackageAnalyzer.EfCorePackageId),
            new XAttribute("Version", EfCorePackageVersion));

        if (targetItemGroup is not null)
        {
            targetItemGroup.Add(packageReference);
        }
        else
        {
            doc.Root.Add(new XElement(doc.Root.GetDefaultNamespace() + "ItemGroup", packageReference));
        }

        doc.Save(csprojPath);
        // We return the original solution unchanged — the IDE picks up the
        // csproj change on its own and restores the package on the next build.
        return Task.FromResult(project.Solution);
    }
#pragma warning restore RS1035
}
