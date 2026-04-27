using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// ASP.NET Core's schema generation can't infer the element type of an
// `IEnumerable<T>` when `T` carries a custom JsonConverter (every strong-type
// wrapper does), so the generated schema is `{ "type": "array" }` with no
// `items`. This transformer fills in the missing items with the wire schema
// the converter actually emits — without inflating the array with minItems,
// which is the contract the parameterless IEnumerable position promises.
public sealed class StrongTypeEnumerableItemsTransformer : IOpenApiSchemaTransformer
{
    private static readonly HashSet<Type> s_strongTypeDefinitions =
    [
        typeof(Positive<>),
        typeof(NonNegative<>),
        typeof(Negative<>),
        typeof(NonPositive<>),
        typeof(NonEmptyEnumerable<>),
        typeof(INonEmptyEnumerable<>),
        typeof(Maybe<>),
    ];

    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType) return;

        var def = type.GetGenericTypeDefinition();
        if (def != typeof(IEnumerable<>)) return;

        if (schema.Items is not null) return;

        var elementType = type.GetGenericArguments()[0];
        if (!IsStrongType(elementType)) return;

        var itemsSchema = await context.GetOrCreateSchemaAsync(elementType, parameterDescription: null, cancellationToken);
        schema.Items = itemsSchema;
    }

    private static bool IsStrongType(Type t)
    {
        if (t == typeof(NonEmptyString)) return true;
        if (!t.IsGenericType) return false;
        return s_strongTypeDefinitions.Contains(t.GetGenericTypeDefinition());
    }
}
