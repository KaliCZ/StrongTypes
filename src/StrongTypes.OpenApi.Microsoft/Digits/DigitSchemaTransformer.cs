using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

public sealed class DigitSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(Digit))
            return Task.CompletedTask;

        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = JsonSchemaType.Integer;
        schema.Format = "int32";
        SchemaPaint.TightenLowerBound(schema, 0, floorExclusive: false);
        SchemaPaint.TightenUpperBound(schema, 9, floorExclusive: false);
        StrongTypeInlineMarker.Set(schema);
        return Task.CompletedTask;
    }
}
