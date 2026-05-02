using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrongTypes.Analyzers;

/// <summary>
/// Fires when a project references one of ASP.NET Core's OpenAPI generators
/// (<c>Microsoft.AspNetCore.OpenApi</c> or <c>Swashbuckle.AspNetCore</c>) and
/// exposes any public property whose CLR type is a StrongTypes wrapper, but
/// doesn't reference the matching <c>Kalicz.StrongTypes.OpenApi.*</c> adapter.
/// Without the adapter the generator describes the wrappers by their raw CLR
/// shape — <c>NonEmptyString</c> as <c>{ Value }</c>, <c>Positive&lt;int&gt;</c>
/// as a wrapper object — and generated clients see nonsense schemas that
/// don't match the wire JSON the converters actually emit.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingOpenApiPackageAnalyzer : DiagnosticAnalyzer
{
    public const string MicrosoftDiagnosticId = "ST0002";
    public const string SwashbuckleDiagnosticId = "ST0003";

    public const string MicrosoftAdapterPackageId = "Kalicz.StrongTypes.OpenApi.Microsoft";
    public const string SwashbuckleAdapterPackageId = "Kalicz.StrongTypes.OpenApi.Swashbuckle";

    private const string MicrosoftGeneratorAssembly = "Microsoft.AspNetCore.OpenApi";
    private const string SwashbuckleGeneratorAssembly = "Swashbuckle.AspNetCore.SwaggerGen";

    private static readonly DiagnosticDescriptor MicrosoftRule = new(
        id: MicrosoftDiagnosticId,
        title: $"Install {MicrosoftAdapterPackageId} to describe StrongTypes in your OpenAPI document",
        messageFormat: "Type '{0}' has a StrongTypes wrapper property; install " + MicrosoftAdapterPackageId + " so Microsoft.AspNetCore.OpenApi describes the wire JSON correctly",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: $"{MicrosoftAdapterPackageId} ships the IOpenApiSchemaTransformer / IOpenApiDocumentTransformer pipeline that rewrites the generated schema for every StrongTypes wrapper to match the JSON its converter actually emits. Without it, the document describes the raw CLR shape and consumers see schemas that don't round-trip.",
        helpLinkUri: "https://www.nuget.org/packages/" + MicrosoftAdapterPackageId);

    private static readonly DiagnosticDescriptor SwashbuckleRule = new(
        id: SwashbuckleDiagnosticId,
        title: $"Install {SwashbuckleAdapterPackageId} to describe StrongTypes in your OpenAPI document",
        messageFormat: "Type '{0}' has a StrongTypes wrapper property; install " + SwashbuckleAdapterPackageId + " so Swashbuckle describes the wire JSON correctly",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: $"{SwashbuckleAdapterPackageId} ships the ISchemaFilter / IDocumentFilter pipeline that rewrites the generated schema for every StrongTypes wrapper to match the JSON its converter actually emits. Without it, the document describes the raw CLR shape and consumers see schemas that don't round-trip.",
        helpLinkUri: "https://www.nuget.org/packages/" + SwashbuckleAdapterPackageId);

    private static readonly ImmutableHashSet<string> StrongTypeMetadataNames = ImmutableHashSet.Create(
        "StrongTypes.NonEmptyString",
        "StrongTypes.Positive`1",
        "StrongTypes.NonNegative`1",
        "StrongTypes.Negative`1",
        "StrongTypes.NonPositive`1",
        "StrongTypes.NonEmptyEnumerable`1",
        "StrongTypes.INonEmptyEnumerable`1",
        "StrongTypes.Maybe`1");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MicrosoftRule, SwashbuckleRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStart =>
        {
            var compilation = compilationStart.Compilation;

            var microsoftMissing = ReferencesAssembly(compilation, MicrosoftGeneratorAssembly)
                && !ReferencesAssembly(compilation, "StrongTypes.OpenApi.Microsoft")
                && !ReferencesAssembly(compilation, MicrosoftAdapterPackageId);

            var swashbuckleMissing = ReferencesAssembly(compilation, SwashbuckleGeneratorAssembly)
                && !ReferencesAssembly(compilation, "StrongTypes.OpenApi.Swashbuckle")
                && !ReferencesAssembly(compilation, SwashbuckleAdapterPackageId);

            if (!microsoftMissing && !swashbuckleMissing) return;

            compilationStart.RegisterSymbolAction(symbolContext =>
            {
                var property = (IPropertySymbol)symbolContext.Symbol;
                if (property.DeclaredAccessibility != Accessibility.Public) return;
                if (property.ContainingType is not { DeclaredAccessibility: Accessibility.Public } containingType) return;
                if (!IsStrongType(property.Type)) return;

                foreach (var location in property.Locations)
                {
                    if (microsoftMissing)
                        symbolContext.ReportDiagnostic(Diagnostic.Create(MicrosoftRule, location, containingType.Name));
                    if (swashbuckleMissing)
                        symbolContext.ReportDiagnostic(Diagnostic.Create(SwashbuckleRule, location, containingType.Name));
                }
            }, SymbolKind.Property);
        });
    }

    private static bool ReferencesAssembly(Compilation compilation, string assemblySimpleName)
    {
        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (string.Equals(reference.Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsStrongType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nt && nt.IsGenericType && nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            type = nt.TypeArguments[0];

        if (type is not INamedTypeSymbol named) return false;

        var definition = named.OriginalDefinition;
        var ns = definition.ContainingNamespace?.ToDisplayString();
        var metadataName = definition.IsGenericType
            ? $"{ns}.{definition.Name}`{definition.TypeParameters.Length}"
            : $"{ns}.{definition.Name}";
        return StrongTypeMetadataNames.Contains(metadataName);
    }
}
