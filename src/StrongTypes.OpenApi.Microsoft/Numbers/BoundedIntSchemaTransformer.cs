using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Rewrites the schema for <see cref="BoundedInt{TBounds}"/> to the underlying
/// integer with the witness type's inclusive bounds — e.g. for
/// <c>BoundedInt&lt;PageSizeBounds&gt;</c> with a 1..100 range:
/// <code>
/// { "type": "integer", "format": "int32", "minimum": 1, "maximum": 100 }
/// </code>
/// </summary>
public sealed class BoundedIntSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (StrongTypeSchemaTypes.TryGetBoundedInt(context.JsonTypeInfo.Type, out var valueType, out var min, out var max))
        {
            NumericWrapperPainter.PaintRange(schema, valueType, min, max);
            StrongTypeInlineMarker.Set(schema);
        }

        return Task.CompletedTask;
    }
}
