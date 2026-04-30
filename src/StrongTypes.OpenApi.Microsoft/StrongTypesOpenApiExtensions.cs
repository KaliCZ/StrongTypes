using Microsoft.AspNetCore.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>Entry point for wiring StrongTypes schema transformers into <c>Microsoft.AspNetCore.OpenApi</c>.</summary>
public static class StrongTypesOpenApiExtensions
{
    /// <summary>Registers schema transformers so <see cref="NonEmptyString"/>, the numeric strong-type wrappers, <see cref="NonEmptyEnumerable{T}"/>/<see cref="INonEmptyEnumerable{T}"/>, and <see cref="Maybe{T}"/> render in the generated OpenAPI document as the JSON shape their converters actually produce.</summary>
    /// <param name="options">The OpenAPI options being configured.</param>
    public static OpenApiOptions AddStrongTypes(this OpenApiOptions options)
    {
        options.AddSchemaTransformer<NonEmptyStringSchemaTransformer>();
        options.AddSchemaTransformer<NumericStrongTypeSchemaTransformer>();
        options.AddSchemaTransformer<NonEmptyEnumerableSchemaTransformer>();
        options.AddSchemaTransformer<MaybeSchemaTransformer>();
        options.AddSchemaTransformer<StrongTypeCollectionShapeTransformer>();
        options.AddDocumentTransformer<StrongTypesComponentSchemaFiller>();
        options.AddDocumentTransformer<PropertyAnnotationSchemaTransformer>();
        return options;
    }
}
