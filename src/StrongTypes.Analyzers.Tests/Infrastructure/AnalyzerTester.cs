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

        AssertCompiles(compilation);

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>
    /// An analyzer reads a broken tree without complaining and simply finds less in it, so a source
    /// missing a reference yields no diagnostics — and every <c>Silent_</c> test would pass for the
    /// wrong reason. Fail loudly instead.
    /// </summary>
    private static void AssertCompiles(CSharpCompilation compilation)
    {
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Test source does not compile, so any assertion about what the analyzer reports is meaningless:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }
}
