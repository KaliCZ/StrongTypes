using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace StrongTypes.Analyzers;

/// <summary>
/// Fires when an options type carrying a strong type that must be configured is bound with
/// <c>Bind</c> / <c>Configure</c>, which cannot notice the key is missing.
/// </summary>
/// <remarks>
/// A wrapper's invariant constrains every value it can hold; it cannot make the binder assign one.
/// An unconfigured <c>NonEmptyString</c> is therefore <c>null</c> and an unconfigured
/// <c>Positive&lt;int&gt;</c> is <c>1</c> — its default, an ordinary invariant-satisfying value.
/// <c>ValidateOnStart()</c> does not help: binding an absent key succeeds without assigning, so
/// nothing is raised.
/// <para>
/// Only reported where <c>[Required]</c> would not already cover it — a struct wrapper, whose
/// default is never null and so always passes <c>[Required]</c>, or a reference wrapper with no
/// <c>[Required]</c> at all.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnvalidatedStrongTypeOptionsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0004";
    public const string ConfigurationPackageId = "Kalicz.StrongTypes.Configuration";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Bind strong-typed options with BindStrongTypes",
        messageFormat: "Options type '{0}' requires configuration for {1}; bind with BindStrongTypes() so a missing key fails instead of silently defaulting",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Binding an absent configuration key succeeds without assigning, so a non-nullable strong type keeps a default: null for a reference wrapper, and for a struct wrapper an ordinary value ([Required] cannot see that default(Positive<int>) is 1 rather than a configured 1). Kalicz.StrongTypes.Configuration's BindStrongTypes() checks the section for each required key instead, taking required-ness from the declaration — Positive<int> is required, Positive<int>? is optional.",
        helpLinkUri: "https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration");

    private static readonly ImmutableHashSet<string> StrongTypeMetadataNames = ImmutableHashSet.Create(
        "StrongTypes.NonEmptyString",
        "StrongTypes.Email",
        "StrongTypes.Digit",
        "StrongTypes.Positive`1",
        "StrongTypes.NonNegative`1",
        "StrongTypes.Negative`1",
        "StrongTypes.NonPositive`1");

    // The two shapes that bind a whole options type from a section and have a
    // BindStrongTypes equivalent. ConfigurationBinder.Get<T>/Bind(object) are
    // deliberately excluded: they are one-off reads with no OptionsBuilder to fix.
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

                var unguarded = CollectPropertiesNeedingConfiguration(optionsType, requiredAttribute);
                if (unguarded.Count == 0)
                {
                    return;
                }

                operationContext.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    optionsType.Name,
                    string.Join(", ", unguarded.Select(p => p.Name))));
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

    /// <summary>Properties whose absence nothing would notice: a struct wrapper (whose default satisfies <c>[Required]</c>) or an unannotated-as-nullable reference wrapper without <c>[Required]</c>.</summary>
    private static List<IPropertySymbol> CollectPropertiesNeedingConfiguration(INamedTypeSymbol optionsType, INamedTypeSymbol? requiredAttribute)
    {
        var result = new List<IPropertySymbol>();
        for (var current = optionsType; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IPropertySymbol property || property.SetMethod is null || property.IsIndexer)
                {
                    continue;
                }
                if (!IsStrongType(property.Type) || IsOptional(property))
                {
                    continue;
                }
                // A struct wrapper's default is never null, so [Required] always passes on it.
                if (!property.Type.IsValueType && HasRequiredAttribute(property, requiredAttribute))
                {
                    continue;
                }
                result.Add(property);
            }
        }
        return result;
    }

    private static bool HasRequiredAttribute(IPropertySymbol property, INamedTypeSymbol? requiredAttribute) =>
        requiredAttribute is not null
        && property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, requiredAttribute));

    /// <summary>An assembly compiled without nullable reference types annotates nothing, so a reference wrapper reads as <see cref="NullableAnnotation.None"/> — no intent declared, nothing to enforce.</summary>
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
