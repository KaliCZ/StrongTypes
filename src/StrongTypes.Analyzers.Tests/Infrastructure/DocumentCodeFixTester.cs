using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

/// <summary>
/// Drives a code fix that edits source, as opposed to <see cref="CodeFixTester"/>, which exists for
/// the package-install fixes that write to a csproj and so cannot be observed as a document change.
/// Diagnostics come from really running the analyzer, so a fix cannot be tested against a location
/// the analyzer would never report.
/// </summary>
internal static class DocumentCodeFixTester
{
    public static async Task<IReadOnlyList<CodeAction>> RegisterFixesAsync(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider provider,
        string source,
        IEnumerable<MetadataReference> references)
    {
        var (document, diagnostics) = await AnalyseAsync(analyzer, source, references);
        if (diagnostics.IsEmpty)
        {
            return [];
        }

        var registered = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics[0], (action, _) => registered.Add(action), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);
        return registered;
    }

    /// <summary>Applies the first offered fix and returns the resulting source.</summary>
    public static async Task<string> ApplySingleFixAsync(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider provider,
        string source,
        IEnumerable<MetadataReference> references)
    {
        var (document, diagnostics) = await AnalyseAsync(analyzer, source, references);
        if (diagnostics.IsEmpty)
        {
            throw new InvalidOperationException("The analyzer reported nothing, so there is no fix to apply.");
        }

        var registered = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics[0], (action, _) => registered.Add(action), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);

        var operations = await registered.Single().GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var text = await changed.GetDocument(document.Id)!.GetTextAsync();
        return text.ToString();
    }

    private static async Task<(Document Document, ImmutableArray<Diagnostic> Diagnostics)> AnalyseAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        IEnumerable<MetadataReference> references)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Target", "Target", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, references)
            .AddDocument(documentId, "Test.cs", source);

        var document = solution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync();
        var diagnostics = await compilation!
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync();

        return (document, diagnostics);
    }
}
