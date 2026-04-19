using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

/// <summary>
/// Drives a <see cref="DiagnosticAnalyzer"/> against an in-memory compilation and returns the
/// diagnostics it produced. Kept deliberately thin — no `Microsoft.CodeAnalysis.Testing`
/// dependency, so tests stay portable across xUnit v2/v3 and Roslyn versions.
/// </summary>
internal static class AnalyzerTester
{
    public static async Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        IEnumerable<MetadataReference> references,
        string assemblyName = "TestAssembly")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
