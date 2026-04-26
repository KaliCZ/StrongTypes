using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>Rewrites the <see cref="NonEmptyString"/> schema to <c>{ "type": "string", "minLength": 1 }</c> so it matches the JSON the converter reads and writes.</summary>
public sealed class NonEmptyStringSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(NonEmptyString))
            return Task.CompletedTask;

        StrongTypesSchemaReset.ResetToScalar(schema);
        schema.Type = JsonSchemaType.String;
        schema.MinLength = 1;
        return Task.CompletedTask;
    }
}
