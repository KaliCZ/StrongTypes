using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace StrongTypes.EfCore;

/// <summary>Translates <c>Unwrap()</c> calls on strong types to the underlying column in generated SQL.</summary>
public sealed class UnwrapMethodCallTranslator(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslator
{
    // Every strong-type's extensions class exposes a static Unwrap(this Self) →
    // underlying. Non-generic for NonEmptyString (hand-written), generic with a
    // single type parameter for the numeric wrappers (source-generated).
    private static readonly HashSet<MethodInfo> UnwrapMethodDefinitions =
    [
        UnwrapOn(typeof(NonEmptyStringExtensions)),
        UnwrapOn(typeof(PositiveExtensions)),
        UnwrapOn(typeof(NonNegativeExtensions)),
        UnwrapOn(typeof(NegativeExtensions)),
        UnwrapOn(typeof(NonPositiveExtensions)),
    ];

    private static MethodInfo UnwrapOn(Type extensionsType) =>
        extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(m => m.Name == nameof(NonEmptyStringExtensions.Unwrap) && m.GetParameters().Length == 1)
        ?? throw new InvalidOperationException($"No single-parameter Unwrap method found on {extensionsType}.");

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        if (!UnwrapMethodDefinitions.Contains(definition))
        {
            return null;
        }

        var underlyingClrType = method.ReturnType;
        var underlyingMapping = typeMappingSource.FindMapping(underlyingClrType)
            ?? throw new InvalidOperationException($"No RelationalTypeMapping registered for {underlyingClrType}.");
        return sqlExpressionFactory.Convert(arguments[0], underlyingClrType, underlyingMapping);
    }
}

public sealed class UnwrapMethodCallTranslatorPlugin(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslatorPlugin
{
    public IEnumerable<IMethodCallTranslator> Translators { get; } =
        [new UnwrapMethodCallTranslator(sqlExpressionFactory, typeMappingSource)];
}
