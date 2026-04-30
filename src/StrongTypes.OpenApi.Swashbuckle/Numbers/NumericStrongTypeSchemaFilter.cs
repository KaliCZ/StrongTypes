using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for the numeric strong-type wrappers
/// <see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>,
/// <see cref="Negative{T}"/>, and <see cref="NonPositive{T}"/> to the
/// underlying primitive with the appropriate bound — e.g. for
/// <c>Positive&lt;int&gt;</c>:
/// <code>
/// { "type": "integer", "format": "int32", "exclusiveMinimum": "0" }
/// </code>
/// Caller-supplied <c>[Range]</c> annotations are preserved when at least
/// as tight as the wrapper's floor.
/// </summary>
public sealed class NumericStrongTypeSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsGenericType) return;
        if (schema is not OpenApiSchema concrete) return;

        if (NumericWrapperKinds.TryGetBound(type.GetGenericTypeDefinition(), out var bound))
            NumericWrapperPainter.Paint(concrete, type.GetGenericArguments()[0], bound);
    }
}
