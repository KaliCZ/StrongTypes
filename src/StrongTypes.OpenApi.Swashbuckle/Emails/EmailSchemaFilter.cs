using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

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
public sealed class EmailSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!StrongTypeSchemaTypes.IsEmail(context.Type)) return;
        if (schema is not OpenApiSchema concrete) return;

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.String;
        SchemaPaint.SetFormatIfAbsent(concrete, "email");
        SchemaPaint.TightenMinLength(concrete, 1);
        SchemaPaint.TightenMaxLength(concrete, Email.MaxLength);
        StrongTypeInlineMarker.Set(concrete);
    }
}
