using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

/// <summary>
/// Drives a <see cref="CodeFixProvider"/> against a real csproj on disk. The EfCore package code
/// fix bypasses Roslyn's document model and writes to the csproj directly, so the standard
/// `CSharpCodeFixTest` scaffolding (which diffs <see cref="Solution"/> state) can't observe it —
/// we need to point a real <see cref="Project"/> at a temp file, invoke the registered
/// <see cref="CodeAction"/>, and assert against the file contents afterwards.
/// </summary>
internal static class CodeFixTester
{
    public static async Task<IReadOnlyList<CodeAction>> RegisterFixesAsync(
        CodeFixProvider provider,
        string csprojPath,
        string diagnosticId = MissingEfCorePackageAnalyzer.DiagnosticId)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: "Target",
                assemblyName: "Target",
                language: LanguageNames.CSharp,
                filePath: csprojPath))
            .AddDocument(
                DocumentId.CreateNewId(projectId),
                name: "Dummy.cs",
                text: "namespace N;",
                filePath: Path.Combine(Path.GetDirectoryName(csprojPath)!, "Dummy.cs"));

        var project = solution.GetProject(projectId)!;
        var document = project.Documents.Single();
        var tree = CSharpSyntaxTree.ParseText("namespace N;");
        var descriptor = new DiagnosticDescriptor(
            diagnosticId,
            title: "placeholder",
            messageFormat: "placeholder",
            category: "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        var diagnostic = Diagnostic.Create(
            descriptor,
            Location.Create(tree, new Microsoft.CodeAnalysis.Text.TextSpan(0, 0)));

        var registered = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => registered.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(context);
        return registered;
    }

    public static async Task ApplyAsync(CodeAction action)
    {
        // The provider swaps `createChangedSolution` for a no-op return; the side effect is the
        // disk write done inside that delegate, not a Solution diff. Still, driving through the
        // public API is the right integration point — that's what the IDE does.
        _ = await action.GetOperationsAsync(CancellationToken.None);
    }
}
