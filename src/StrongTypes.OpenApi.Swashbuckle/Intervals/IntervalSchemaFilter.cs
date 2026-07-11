using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for the four interval types
/// (<see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>,
/// <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>) to the
/// on-the-wire object the JSON converter emits:
/// <code>
/// { "type": "object", "properties": { "Start": &lt;T&gt;, "End": &lt;T&gt;, "StartInclusive": bool, "EndInclusive": bool }, "required": ["Start", "End"] }
/// </code>
/// Each variant's nullability rides on the endpoint schema — a required endpoint
/// renders non-nullable, an optional one nullable — and only the required
/// endpoints appear in <c>required</c>, since the converter accepts omitting an
/// optional key. The bound flags are optional booleans defaulting to <c>true</c>,
/// matching the converter's omit-when-inclusive wire format.
/// </summary>
public sealed class IntervalSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!StrongTypeSchemaTypes.TryGetIntervalEndpoints(context.Type, out var startType, out var endType)) return;
        if (schema is not OpenApiSchema concrete) return;

        var startSchema = context.SchemaGenerator.GenerateSchema(
            startType, context.SchemaRepository, memberInfo: null, parameterInfo: null, routeInfo: null);
        var endSchema = context.SchemaGenerator.GenerateSchema(
            endType, context.SchemaRepository, memberInfo: null, parameterInfo: null, routeInfo: null);

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.Object;
        concrete.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["Start"] = startSchema,
            ["End"] = endSchema,
            ["StartInclusive"] = BoundFlagSchema(),
            ["EndInclusive"] = BoundFlagSchema(),
        };
        concrete.Required = RequiredEndpoints(startType, endType);
        StrongTypeInlineMarker.Set(concrete);
    }

    private static OpenApiSchema BoundFlagSchema() => new()
    {
        Type = JsonSchemaType.Boolean,
        Default = System.Text.Json.Nodes.JsonValue.Create(true),
    };

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
