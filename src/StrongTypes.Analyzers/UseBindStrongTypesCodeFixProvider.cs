using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrongTypes.Analyzers;

/// <summary>
/// Code fix for <see cref="UnvalidatedStrongTypeOptionsAnalyzer"/>: rewrites <c>.Bind(section)</c> to
/// <c>.BindStrongTypes(section)</c>. Offered only when <c>Kalicz.StrongTypes.Configuration</c> is
/// referenced, and only for <c>Bind</c> — a <c>Configure&lt;T&gt;</c> call needs restructuring, so it
/// gets none.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseBindStrongTypesCodeFixProvider))]
[Shared]
public sealed class UseBindStrongTypesCodeFixProvider : CodeFixProvider
{
    private const string BindStrongTypes = "BindStrongTypes";
    private const string ConfigurationNamespace = "StrongTypes.Configuration";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(UnvalidatedStrongTypeOptionsAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
        if (compilation is null || !ReferencesConfigurationPackage(compilation))
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var invocation = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocation?.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Bind" } access)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use BindStrongTypes()",
                    createChangedDocument: ct => UseBindStrongTypesAsync(context.Document, access, ct),
                    equivalenceKey: nameof(UseBindStrongTypesCodeFixProvider)),
                diagnostic);
        }
    }

    private static bool ReferencesConfigurationPackage(Compilation compilation) =>
        compilation.ReferencedAssemblyNames.Any(a =>
            string.Equals(a.Name, "StrongTypes.Configuration", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a.Name, UnvalidatedStrongTypeOptionsAnalyzer.ConfigurationPackageId, StringComparison.OrdinalIgnoreCase));

    private static async Task<Document> UseBindStrongTypesAsync(
        Document document,
        MemberAccessExpressionSyntax access,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var renamed = access.WithName(SyntaxFactory.IdentifierName(BindStrongTypes).WithTriviaFrom(access.Name));
        var updated = root.ReplaceNode(access, renamed);

        return document.WithSyntaxRoot(AddUsing(updated));
    }

    private static SyntaxNode AddUsing(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax unit)
        {
            return root;
        }
        if (unit.Usings.Any(u => u.Name?.ToString() == ConfigurationNamespace))
        {
            return unit;
        }

        var directive = SyntaxFactory
            .UsingDirective(SyntaxFactory.ParseName(ConfigurationNamespace))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        // Keep the file's usings sorted rather than appending to the end.
        var insertAt = unit.Usings.TakeWhile(u => string.CompareOrdinal(u.Name?.ToString(), ConfigurationNamespace) < 0).Count();
        return unit.WithUsings(unit.Usings.Insert(insertAt, directive));
    }
}
