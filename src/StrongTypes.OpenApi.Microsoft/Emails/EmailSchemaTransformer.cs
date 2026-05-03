using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Rewrites the schema for <see cref="Email"/> to:
/// <code>
/// { "type": "string", "format": "email", "minLength": 1, "maxLength": 254 }
/// </code>
/// matching the JSON the converter reads and writes. Caller-supplied
/// annotations (e.g. <c>[StringLength]</c>, <c>[RegularExpression]</c>)
/// are preserved; <c>minLength</c>/<c>maxLength</c> are only tightened
/// toward the wrapper's bounds, never relaxed.
/// </summary>
public sealed class EmailSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(Email))
            return Task.CompletedTask;

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.String;
        SchemaPaint.SetFormatIfAbsent(schema, "email");
        SchemaPaint.TightenMinLength(schema, 1);
        SchemaPaint.TightenMaxLength(schema, Email.MaxLength);
        StrongTypeInlineMarker.Set(schema);
        return Task.CompletedTask;
    }
}
