using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace StrongTypes.Analyzers;

/// <summary>
/// Flags a <c>Bind</c> / <c>Configure</c> on an options type that would leave a non-nullable
/// reference wrapper null at any depth when the key is missing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnvalidatedStrongTypeOptionsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0004";
    public const string ConfigurationPackageId = "Kalicz.StrongTypes.Configuration";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Bind strong-typed options with BindStrongTypes",
        messageFormat: "Options type '{0}' will bind {1} to null when the key is missing; bind with BindStrongTypes() so that fails instead",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Binding an absent configuration key succeeds without assigning, so a non-nullable reference wrapper is left null — the one thing its type says it can never be — and nothing reports it. Kalicz.StrongTypes.Configuration's BindStrongTypes() fails on it instead, reading which properties are non-nullable from the declaration rather than from [Required] attributes. Struct wrappers are unaffected: an unconfigured Positive<int> is 1, a value the type is happy to hold.",
        helpLinkUri: "https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration");

    private static readonly ImmutableHashSet<string> StrongTypeMetadataNames = ImmutableHashSet.Create(
        "StrongTypes.NonEmptyString",
        "StrongTypes.Email",
        "StrongTypes.Digit",
        "StrongTypes.Positive`1",
        "StrongTypes.NonNegative`1",
        "StrongTypes.Negative`1",
        "StrongTypes.NonPositive`1");

    // Get<T> / Bind(object) are excluded: one-off reads with no OptionsBuilder to rewrite to.
    private const string OptionsBuilderBindExtensions = "Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions";
    private const string ServiceCollectionConfigureExtensions = "Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStart =>
        {
            var compilation = compilationStart.Compilation;

            var bindExtensions = compilation.GetTypeByMetadataName(OptionsBuilderBindExtensions);
            var configureExtensions = compilation.GetTypeByMetadataName(ServiceCollectionConfigureExtensions);
            if (bindExtensions is null && configureExtensions is null)
            {
                return;
            }

            var requiredAttribute = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.RequiredAttribute");

            compilationStart.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                var method = invocation.TargetMethod;

                if (!IsBindingCall(method, bindExtensions, configureExtensions))
                {
                    return;
                }
                if (method.TypeArguments.FirstOrDefault() is not INamedTypeSymbol optionsType)
                {
                    return;
                }

                var unguarded = CollectPropertiesLeftNull(optionsType, requiredAttribute);
                if (unguarded.Count == 0)
                {
                    return;
                }

                operationContext.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    optionsType.Name,
                    string.Join(", ", unguarded)));
            }, OperationKind.Invocation);
        });
    }

    private static bool IsBindingCall(IMethodSymbol method, INamedTypeSymbol? bindExtensions, INamedTypeSymbol? configureExtensions)
    {
        if (method.TypeArguments.Length != 1)
        {
            return false;
        }
        var containing = method.ContainingType;
        if (method.Name == "Bind" && bindExtensions is not null && SymbolEqualityComparer.Default.Equals(containing, bindExtensions))
        {
            return true;
        }
        return method.Name == "Configure"
               && configureExtensions is not null
               && SymbolEqualityComparer.Default.Equals(containing, configureExtensions);
    }

    private static List<string> CollectPropertiesLeftNull(INamedTypeSymbol optionsType, INamedTypeSymbol? requiredAttribute)
    {
        var result = new List<string>();
        Walk(optionsType, prefix: "", result, requiredAttribute, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
        return result;
    }

    private static void Walk(
        INamedTypeSymbol type,
        string prefix,
        List<string> result,
        INamedTypeSymbol? requiredAttribute,
        HashSet<INamedTypeSymbol> visited)
    {
        // A type reachable by two paths is reported once; without this a cycle would not terminate.
        if (!visited.Add(type))
        {
            return;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IPropertySymbol property || property.SetMethod is null || property.IsIndexer)
                {
                    continue;
                }

                var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;

                if (IsStrongType(property.Type))
                {
                    // A value type has no invalid state to reach, so an absent key leaves nothing wrong.
                    if (property.Type.IsValueType
                        || IsOptional(property)
                        || HasRequiredAttribute(property, requiredAttribute))
                    {
                        continue;
                    }
                    result.Add(path);
                    continue;
                }

                if (BindsAsAnObjectGraph(property.Type) is { } nested)
                {
                    Walk(nested, path, result, requiredAttribute, visited);
                }
            }
        }
    }

    /// <summary>The type to recurse into, or <c>null</c> where the binder would convert a scalar instead. Unwraps arrays and collections to their element type.</summary>
    private static INamedTypeSymbol? BindsAsAnObjectGraph(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return BindsAsAnObjectGraph(array.ElementType);
        }
        if (type is not INamedTypeSymbol named || named.SpecialType != SpecialType.None)
        {
            return null;
        }
        if (named.IsGenericType && ElementOf(named) is { } element)
        {
            return BindsAsAnObjectGraph(element);
        }
        if (named.TypeKind is not (TypeKind.Class or TypeKind.Struct) || HasTypeConverter(named))
        {
            return null;
        }
        return named.ContainingNamespace?.ToDisplayString().StartsWith("System", StringComparison.Ordinal) == true
            ? null
            : named;
    }

    private static ITypeSymbol? ElementOf(INamedTypeSymbol type) =>
        type.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            ? type.TypeArguments.LastOrDefault()
            : null;

    private static bool HasTypeConverter(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.ComponentModel.TypeConverterAttribute"))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasRequiredAttribute(IPropertySymbol property, INamedTypeSymbol? requiredAttribute) =>
        requiredAttribute is not null
        && property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, requiredAttribute));

    /// <summary>Also true for a reference wrapper in an assembly compiled without NRT: no annotation, so no intent to enforce.</summary>
    private static bool IsOptional(IPropertySymbol property)
    {
        if (property.Type is INamedTypeSymbol named
            && named.IsGenericType
            && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }
        if (property.Type.IsValueType)
        {
            return false;
        }
        return property.NullableAnnotation != NullableAnnotation.NotAnnotated;
    }

    private static bool IsStrongType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nullable
            && nullable.IsGenericType
            && nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            type = nullable.TypeArguments[0];
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
