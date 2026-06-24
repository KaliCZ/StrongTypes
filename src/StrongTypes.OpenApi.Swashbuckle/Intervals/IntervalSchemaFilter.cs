using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

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
        };
        concrete.Required = new HashSet<string>(StringComparer.Ordinal) { "Start", "End" };
        StrongTypeInlineMarker.Set(concrete);
    }
}
