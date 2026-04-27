using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>Rewrites the schema for <see cref="NonEmptyEnumerable{T}"/> and <see cref="INonEmptyEnumerable{T}"/> to <c>{ "type": "array", "minItems": 1, "items": &lt;T schema&gt; }</c>.</summary>
public sealed class NonEmptyEnumerableSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType) return;

        var definition = type.GetGenericTypeDefinition();
        if (definition != typeof(NonEmptyEnumerable<>) && definition != typeof(INonEmptyEnumerable<>))
            return;

        var elementType = type.GetGenericArguments()[0];
        var itemsSchema = await context.GetOrCreateSchemaAsync(elementType, parameterDescription: null, cancellationToken);

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.Array;
        SchemaPaint.TightenMinItems(schema, 1);
        schema.Items = itemsSchema;
    }
}
