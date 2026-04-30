using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// ASP.NET Core's schema generation cannot infer the element/value schema
// for a CLR collection whose element type carries a custom JsonConverter
// (every strong-type wrapper does), so the generated schema is left with
// no `items` for arrays and no `additionalProperties` for dictionaries.
// This transformer fills those positions in for any type the serializer
// recognises as Enumerable or Dictionary, regardless of the concrete CLR
// shape (`List<T>`, `T[]`, `FrozenSet<T>`, `IDictionary<,>`,
// `FrozenDictionary<,>`, `SortedList<,>`, …). The strong-typed-key
// dictionary case never reaches this hook (the framework inlines a
// degenerate schema for it without invoking schema transformers); see
// <see cref="StrongTypeKeyedDictionaryFallback"/> for the document-level
// patch that handles it.
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
