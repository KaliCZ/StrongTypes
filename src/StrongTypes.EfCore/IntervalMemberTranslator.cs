using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace StrongTypes.EfCore;

/// <summary>Translates <c>Start</c>/<c>End</c> access on a JSON-column-mapped interval to a server-side JSON path lookup, so endpoint predicates, ordering, and projections run in SQL.</summary>
public sealed class IntervalMemberTranslator(IRelationalTypeMappingSource typeMappingSource) : IMemberTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null
            || member.Name is not ("Start" or "End")
            || member.DeclaringType is not { } declaringType
            || !IntervalTypes.IsInterval(declaringType))
        {
            return null;
        }
        var endpointType = Nullable.GetUnderlyingType(returnType) ?? returnType;
        if (typeMappingSource.FindMapping(endpointType) is not { } endpointMapping)
        {
            return null;
        }
        return new JsonScalarExpression(instance, [new PathSegment(member.Name)], endpointType, endpointMapping, nullable: true);
    }
}

public sealed class IntervalMemberTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource) : IMemberTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } = [new IntervalMemberTranslator(typeMappingSource)];
}
