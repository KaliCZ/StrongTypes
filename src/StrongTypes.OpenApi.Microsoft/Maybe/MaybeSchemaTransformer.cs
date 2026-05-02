using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Rewrites the schema for <see cref="Maybe{T}"/> to the on-the-wire
/// wrapper object the JSON converter emits:
/// <code>
/// { "type": "object", "properties": { "Value": &lt;T schema&gt; } }
/// </code>
/// The <c>Value</c> property is not required — omitting it is how the
/// converter encodes <c>None</c>.
/// </summary>
public sealed class MaybeSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Maybe<>))
            return;

        var innerType = type.GetGenericArguments()[0];
        var innerSchema = await context.GetOrCreateSchemaAsync(innerType, parameterDescription: null, cancellationToken);

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.Object;
        schema.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["Value"] = innerSchema,
        };
        StrongTypeInlineMarker.Set(schema);
    }
}
