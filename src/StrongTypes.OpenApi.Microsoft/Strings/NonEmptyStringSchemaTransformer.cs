using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Rewrites the schema for <see cref="NonEmptyString"/> to:
/// <code>
/// { "type": "string", "minLength": 1 }
/// </code>
/// matching the JSON the converter reads and writes. Caller-supplied
/// annotations (e.g. <c>[StringLength]</c>, <c>[RegularExpression]</c>)
/// are preserved; <c>minLength</c> is only strengthened toward the floor
/// of <c>1</c>, never weakened.
/// </summary>
public sealed class NonEmptyStringSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(NonEmptyString))
            return Task.CompletedTask;

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.String;
        SchemaPaint.TightenMinLength(schema, 1);
        StrongTypeInlineMarker.Set(schema);
        return Task.CompletedTask;
    }
}
