using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrongTypes.Analyzers;

/// <summary>
/// Fires when a project references <c>Microsoft.EntityFrameworkCore</c> and maps
/// an entity that carries a StrongTypes wrapper property, but doesn't reference
/// the <c>Kalicz.StrongTypes.EfCore</c> package that supplies the value
/// converters and LINQ translator. Without it, EF Core will try to treat the
/// wrapper as an owned entity type and blow up at model-build time with a
/// "no suitable constructor" error.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingEfCorePackageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Install Kalicz.StrongTypes.EfCore to persist strong types",
        messageFormat: "Entity '{0}' has a StrongTypes wrapper property; install Kalicz.StrongTypes.EfCore so EF Core can persist it",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Kalicz.StrongTypes.EfCore ships value converters and the Unwrap() LINQ translator needed for EF Core to round-trip NonEmptyString, Positive<T>, NonNegative<T>, Negative<T>, and NonPositive<T> to a database column. Without the package, EF Core infers the wrapper as an owned entity type and fails at model-build time.",
        helpLinkUri: "https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore");

    // A single canonical name for each wrapper. Generic ones use the CLR
    // metadata-name form (backtick + arity) so compiled assemblies match.
    private static readonly ImmutableHashSet<string> StrongTypeFullNames = ImmutableHashSet.Create(
        "StrongTypes.NonEmptyString",
        "StrongTypes.Positive`1",
        "StrongTypes.NonNegative`1",
        "StrongTypes.Negative`1",
        "StrongTypes.NonPositive`1");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStart =>
        {
            var compilation = compilationStart.Compilation;

            // Only relevant when EF Core is in the graph and the EfCore package isn't.
            if (!ReferencesAssembly(compilation, "Microsoft.EntityFrameworkCore"))
            {
                return;
            }
            if (ReferencesAssembly(compilation, "StrongTypes.EfCore") ||
                ReferencesAssembly(compilation, "Kalicz.StrongTypes.EfCore"))
            {
                return;
            }

            var dbContextSymbol = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            var dbSetSymbol = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");
            if (dbContextSymbol is null || dbSetSymbol is null)
            {
                return;
            }

            // Cache per compilation so we only walk an entity's members once.
            var reportedEntities = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            compilationStart.RegisterSymbolAction(symbolContext =>
            {
                var property = (IPropertySymbol)symbolContext.Symbol;
                if (property.Type is not INamedTypeSymbol propertyType) return;
                if (!SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, dbSetSymbol)) return;
                if (!InheritsFrom(property.ContainingType, dbContextSymbol)) return;

                if (propertyType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol entityType) return;
                if (!reportedEntities.Add(entityType)) return;

                if (!HasStrongTypeProperty(entityType)) return;

                foreach (var location in property.Locations)
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, location, entityType.Name));
                }
            }, SymbolKind.Property);
        });
    }

    private static bool ReferencesAssembly(Compilation compilation, string assemblySimpleName)
    {
        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (string.Equals(reference.Name, assemblySimpleName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasStrongTypeProperty(INamedTypeSymbol entityType)
    {
        for (var current = entityType; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol p && IsStrongType(p.Type))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsStrongType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named) return false;
        var metadataName = named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
        // For generics, ToDisplayString gives StrongTypes.Positive<T> — normalize
        // to the metadata form StrongTypes.Positive`1 that our name set uses.
        if (named.IsGenericType)
        {
            metadataName = $"{named.ContainingNamespace?.ToDisplayString()}.{named.Name}`{named.TypeParameters.Length}";
        }
        return StrongTypeFullNames.Contains(metadataName);
    }
}
