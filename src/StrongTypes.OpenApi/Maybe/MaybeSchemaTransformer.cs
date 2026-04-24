using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace StrongTypes.OpenApi;

/// <summary>Rewrites the schema for <see cref="Maybe{T}"/> to the on-the-wire wrapper object the JSON converter emits: <c>{ "Value": &lt;T schema&gt; }</c>. The <c>Value</c> property is not required — omitting it is how the converter encodes <c>None</c>.</summary>
public sealed class MaybeSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Maybe<>))
            return;

        var innerType = type.GetGenericArguments()[0];
        var innerSchema = await context.GetOrCreateSchemaAsync(innerType, jsonPropertyInfo: null, cancellationToken);

        StrongTypesSchemaReset.ResetToScalar(schema);
        schema.Type = "object";
        schema.Properties = new Dictionary<string, OpenApiSchema>
        {
            ["Value"] = innerSchema,
        };
    }
}
