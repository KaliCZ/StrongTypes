using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for <see cref="Maybe{T}"/> to the on-the-wire
/// wrapper object the JSON converter emits:
/// <code>
/// { "type": "object", "properties": { "Value": &lt;T schema&gt; } }
/// </code>
/// The <c>Value</c> property is not required — omitting it is how the
/// converter encodes <c>None</c>.
/// </summary>
public sealed class MaybeSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!StrongTypeSchemaTypes.TryGetMaybeValue(context.Type, out var innerType)) return;
        if (schema is not OpenApiSchema concrete) return;

        var innerSchema = context.SchemaGenerator.GenerateSchema(
            innerType, context.SchemaRepository, memberInfo: null, parameterInfo: null, routeInfo: null);

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.Object;
        concrete.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["Value"] = innerSchema,
        };
        StrongTypeInlineMarker.Set(concrete);
    }
}
