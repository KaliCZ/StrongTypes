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
/// Each variant's nullability rides on the endpoint schema — a required endpoint
/// renders non-nullable, an optional one nullable — and only the required
/// endpoints appear in <c>required</c>, since the converter accepts omitting an
/// optional key.
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
        schema.Required = RequiredEndpoints(startType, endType);
        StrongTypeInlineMarker.Set(schema);
    }

    // An endpoint is required exactly when its type is the bare value type; an
    // optional endpoint is Nullable<T>, and the converter lets its key be omitted.
    private static HashSet<string> RequiredEndpoints(Type startType, Type endType)
    {
        var required = new HashSet<string>(StringComparer.Ordinal);
        if (Nullable.GetUnderlyingType(startType) is null) required.Add("Start");
        if (Nullable.GetUnderlyingType(endType) is null) required.Add("End");
        return required;
    }
}
