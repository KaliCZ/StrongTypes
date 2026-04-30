using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

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
public sealed class NumericStrongTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType) return Task.CompletedTask;

        if (NumericWrapperKinds.TryGetBound(type.GetGenericTypeDefinition(), out var bound))
            NumericWrapperPainter.Paint(schema, type.GetGenericArguments()[0], bound);

        return Task.CompletedTask;
    }
}
