using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

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
    /// A source missing a reference yields no diagnostics rather than an error, so every
    /// <c>Silent_</c> test would pass for the wrong reason.
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
