using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for <see cref="BoundedInt{TBounds}"/> to the underlying
/// integer with the witness type's inclusive bounds — e.g. for
/// <c>BoundedInt&lt;PageSizeBounds&gt;</c> with a 1..100 range:
/// <code>
/// { "type": "integer", "format": "int32", "minimum": 1, "maximum": 100 }
/// </code>
/// </summary>
public sealed class BoundedIntSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;

        if (StrongTypeSchemaTypes.TryGetBoundedInt(context.Type, out var valueType, out var min, out var max))
        {
            NumericWrapperPainter.PaintRange(concrete, valueType, min, max);
            StrongTypeInlineMarker.Set(concrete);
        }
    }
}
