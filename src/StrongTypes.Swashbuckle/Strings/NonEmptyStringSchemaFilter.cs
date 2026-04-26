using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.Swashbuckle;

/// <summary>Rewrites the <see cref="NonEmptyString"/> schema to <c>{ "type": "string", "minLength": 1 }</c> so it matches the JSON the converter reads and writes.</summary>
public sealed class NonEmptyStringSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(NonEmptyString)) return;
        if (schema is not OpenApiSchema concrete) return;

        StrongTypesSchemaReset.ResetToScalar(concrete);
        concrete.Type = JsonSchemaType.String;
        concrete.MinLength = 1;
    }
}
