using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Rewrites the schema for the four interval types
/// (<see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>,
/// <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>) to the
/// on-the-wire object the JSON converter emits:
/// <code>
/// { "type": "object", "properties": { "Start": &lt;T&gt;, "End": &lt;T&gt; }, "required": ["Start", "End"] }
/// </code>
/// Both keys are always present; each variant's nullability rides on the
/// endpoint schema — a required endpoint renders non-nullable, an optional one
/// nullable.
/// </summary>
public sealed class IntervalSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (!StrongTypeSchemaTypes.TryGetIntervalEndpoints(context.JsonTypeInfo.Type, out var startType, out var endType))
            return;

        var startSchema = await context.GetOrCreateSchemaAsync(startType, parameterDescription: null, cancellationToken);
        var endSchema = await context.GetOrCreateSchemaAsync(endType, parameterDescription: null, cancellationToken);

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.Object;
        schema.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["Start"] = startSchema,
            ["End"] = endSchema,
        };
        schema.Required = new HashSet<string>(StringComparer.Ordinal) { "Start", "End" };
        StrongTypeInlineMarker.Set(schema);
    }
}
