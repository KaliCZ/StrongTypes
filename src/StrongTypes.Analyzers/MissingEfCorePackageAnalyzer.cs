using System;
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
/// converters and LINQ translator.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingEfCorePackageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0001";
    public const string EfCorePackageId = "Kalicz.StrongTypes.EfCore";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Install Kalicz.StrongTypes.EfCore to persist strong types",
        messageFormat: "Entity '{0}' has a StrongTypes wrapper property; install Kalicz.StrongTypes.EfCore so EF Core can persist it",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Kalicz.StrongTypes.EfCore ships value converters and the Unwrap() LINQ translator needed for EF Core to round-trip NonEmptyString, Email, Positive<T>, NonNegative<T>, Negative<T>, and NonPositive<T> to a database column. Without the package, EF Core infers the wrapper as an owned entity type and fails at model-build time.",
        helpLinkUri: "https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore");

    private static readonly ImmutableHashSet<string> StrongTypeMetadataNames = ImmutableHashSet.Create(
        "StrongTypes.NonEmptyString",
        "StrongTypes.Email",
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

            var reportedDbContexts = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var reportedEntityProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

            compilationStart.RegisterSymbolAction(symbolContext =>
            {
                var property = (IPropertySymbol)symbolContext.Symbol;
                if (property.Type is not INamedTypeSymbol propertyType)
                {
                    return;
                }
                if (!SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, dbSetSymbol))
                {
                    return;
                }
                var containingContext = property.ContainingType;
                if (!InheritsFrom(containingContext, dbContextSymbol))
                {
                    return;
                }
                if (propertyType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol entityType)
                {
                    return;
                }

                var strongTypeProperties = CollectStrongTypeProperties(entityType);
                if (strongTypeProperties.Count == 0)
                {
                    return;
                }

                foreach (var location in property.Locations)
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, location, entityType.Name));
                }

                foreach (var entityProperty in strongTypeProperties)
                {
                    if (!reportedEntityProperties.Add(entityProperty))
                    {
                        continue;
                    }
                    foreach (var location in entityProperty.Locations)
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, location, entityType.Name));
                    }
                }

                // Also report on the DbContext class — that's where UseStrongTypes() goes once the package is installed.
                if (reportedDbContexts.Add(containingContext))
                {
                    foreach (var location in containingContext.Locations)
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, location, entityType.Name));
                    }
                }
            }, SymbolKind.Property);
        });
    }

    private static bool ReferencesAssembly(Compilation compilation, string assemblySimpleName)
    {
        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (string.Equals(reference.Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
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

    private static List<IPropertySymbol> CollectStrongTypeProperties(INamedTypeSymbol entityType)
    {
        var result = new List<IPropertySymbol>();
        for (var current = entityType; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol p && IsStrongType(p.Type))
                {
                    result.Add(p);
                }
            }
        }
        return result;
    }

    private static bool IsStrongType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nt && nt.IsGenericType && nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            type = nt.TypeArguments[0];
        }
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }
        var definition = named.OriginalDefinition;
        var ns = definition.ContainingNamespace?.ToDisplayString();
        var metadataName = definition.IsGenericType
            ? $"{ns}.{definition.Name}`{definition.TypeParameters.Length}"
            : $"{ns}.{definition.Name}";
        return StrongTypeMetadataNames.Contains(metadataName);
    }
}
