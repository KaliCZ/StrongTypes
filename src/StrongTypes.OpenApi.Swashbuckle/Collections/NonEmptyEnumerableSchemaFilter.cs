using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for <see cref="NonEmptyEnumerable{T}"/> and
/// <see cref="INonEmptyEnumerable{T}"/> to the array shape the JSON
/// converter emits:
/// <code>
/// { "type": "array", "minItems": 1, "items": &lt;T schema&gt; }
/// </code>
/// </summary>
public sealed class NonEmptyEnumerableSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsGenericType) return;
        if (schema is not OpenApiSchema concrete) return;

        var definition = type.GetGenericTypeDefinition();
        if (definition != typeof(NonEmptyEnumerable<>) && definition != typeof(INonEmptyEnumerable<>))
            return;

        var elementType = type.GetGenericArguments()[0];
        var itemsSchema = context.SchemaGenerator.GenerateSchema(
            elementType, context.SchemaRepository, memberInfo: null, parameterInfo: null, routeInfo: null);

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.Array;
        SchemaPaint.TightenMinItems(concrete, 1);
        concrete.Items = itemsSchema;
    }
}
