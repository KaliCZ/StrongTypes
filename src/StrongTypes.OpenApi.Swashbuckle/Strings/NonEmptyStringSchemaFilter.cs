using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>Rewrites the <see cref="NonEmptyString"/> schema to <c>{ "type": "string", "minLength": 1 }</c> so it matches the JSON the converter reads and writes. Caller-supplied annotations (e.g. <c>[StringLength]</c>, <c>[RegularExpression]</c>) are preserved; <c>minLength</c> is only strengthened toward the floor of <c>1</c>, never weakened.</summary>
public sealed class NonEmptyStringSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(NonEmptyString)) return;
        if (schema is not OpenApiSchema concrete) return;

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.String;
        SchemaPaint.TightenMinLength(concrete, 1);
    }
}
