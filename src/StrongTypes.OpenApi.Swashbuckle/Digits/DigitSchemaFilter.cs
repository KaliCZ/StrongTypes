using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

public sealed class DigitSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!StrongTypeSchemaTypes.IsDigit(context.Type)) return;
        if (schema is not OpenApiSchema concrete) return;

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.Integer;
        concrete.Format = "int32";
        SchemaPaint.TightenLowerBound(concrete, 0, floorExclusive: false);
        SchemaPaint.TightenUpperBound(concrete, 9, floorExclusive: false);
        StrongTypeInlineMarker.Set(concrete);
    }
}
