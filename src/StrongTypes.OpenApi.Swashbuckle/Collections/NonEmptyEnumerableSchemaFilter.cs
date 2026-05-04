using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Layers <c>minItems: 1</c> onto the schema for
/// <see cref="NonEmptyEnumerable{T}"/> and <see cref="INonEmptyEnumerable{T}"/>.
/// Swashbuckle already emits the array shape (the type implements
/// <see cref="ICollection{T}"/>); this filter only adds the non-empty bound.
/// </summary>
public sealed class NonEmptyEnumerableSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;
        if (!StrongTypeSchemaTypes.TryGetNonEmptyEnumerableElement(context.Type, out _)) return;

        SchemaPaint.TightenMinItems(concrete, 1);
        StrongTypeInlineMarker.Set(concrete);
    }
}
