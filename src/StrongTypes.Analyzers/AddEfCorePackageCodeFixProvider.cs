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
/// to the project's csproj.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddEfCorePackageCodeFixProvider))]
[Shared]
public sealed class AddEfCorePackageCodeFixProvider : CodeFixProvider
{
    public const string EfCorePackageVersion = "0.3.0";

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
        // Returning the solution unchanged is correct — the fix is the csproj write, which the IDE picks up on its own.
        return Task.FromResult(project.Solution);
    }
#pragma warning restore RS1035
}
