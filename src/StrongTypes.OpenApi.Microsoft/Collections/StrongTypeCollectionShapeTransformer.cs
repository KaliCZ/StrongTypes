using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// ASP.NET Core's schema generation leaves `items` / `additionalProperties` empty when the
// element type carries a custom JsonConverter (every strong-type wrapper does); fill them in.
// Dictionaries with primitive value types never reach this hook — a Microsoft.AspNetCore.OpenApi defect.
public sealed class StrongTypeCollectionShapeTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var info = context.JsonTypeInfo;
        if (info.ElementType is null) return;

        switch (info.Kind)
        {
            case JsonTypeInfoKind.Enumerable:
                schema.Items = await context.GetOrCreateSchemaAsync(info.ElementType, parameterDescription: null, cancellationToken);
                break;
            case JsonTypeInfoKind.Dictionary:
                schema.AdditionalProperties = await context.GetOrCreateSchemaAsync(info.ElementType, parameterDescription: null, cancellationToken);
                break;
        }
    }
}
