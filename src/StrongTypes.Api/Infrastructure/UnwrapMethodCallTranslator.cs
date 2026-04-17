using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace StrongTypes.Api.Infrastructure;

/// <summary>
/// Translates <see cref="NonEmptyStringExtensions.Unwrap(NonEmptyString)"/> as
/// a pass-through to the underlying string column, but re-typed with a plain
/// <see cref="string"/> mapping. Re-mapping matters: if we returned the column
/// expression with its <see cref="NonEmptyString"/> value-converter mapping
/// intact, downstream string operators (Contains, StartsWith, EF.Functions.Like)
/// would pipe their string arguments through that converter at SQL-parameter
/// bind time and fail with <c>InvalidCastException</c>.
/// </summary>
public sealed class UnwrapMethodCallTranslator(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslator
{
    private static readonly MethodInfo UnwrapMethod =
        typeof(NonEmptyStringExtensions).GetMethod(nameof(NonEmptyStringExtensions.Unwrap), [typeof(NonEmptyString)])
        ?? throw new InvalidOperationException($"{nameof(NonEmptyStringExtensions)}.{nameof(NonEmptyStringExtensions.Unwrap)}(NonEmptyString) not found.");

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (!method.Equals(UnwrapMethod))
        {
            return null;
        }

        var stringMapping = typeMappingSource.FindMapping(typeof(string))
            ?? throw new InvalidOperationException("No RelationalTypeMapping registered for string.");
        return sqlExpressionFactory.Convert(arguments[0], typeof(string), stringMapping);
    }
}

public sealed class UnwrapMethodCallTranslatorPlugin(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslatorPlugin
{
    public IEnumerable<IMethodCallTranslator> Translators { get; } =
        [new UnwrapMethodCallTranslator(sqlExpressionFactory, typeMappingSource)];
}
