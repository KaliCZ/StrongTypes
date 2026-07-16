using Microsoft.AspNetCore.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Entry point for wiring StrongTypes schema transformers into
/// <c>Microsoft.AspNetCore.OpenApi</c>.
/// </summary>
public static class StrongTypesOpenApiExtensions
{
    /// <summary>
    /// Registers schema transformers so <see cref="NonEmptyString"/>, the
    /// numeric strong-type wrappers, <see cref="NonEmptyEnumerable{T}"/> /
    /// <see cref="INonEmptyEnumerable{T}"/>, <see cref="Maybe{T}"/>, and the
    /// interval types (<see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>,
    /// <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>) render in
    /// the generated OpenAPI document as the JSON shape their converters
    /// actually produce.
    /// </summary>
    public static OpenApiOptions AddStrongTypes(this OpenApiOptions options)
    {
        options.AddSchemaTransformer<NonEmptyStringSchemaTransformer>();
        options.AddSchemaTransformer<EmailSchemaTransformer>();
        options.AddSchemaTransformer<DigitSchemaTransformer>();
        options.AddSchemaTransformer<NumericStrongTypeSchemaTransformer>();
        options.AddSchemaTransformer<NonEmptyEnumerableSchemaTransformer>();
        options.AddSchemaTransformer<MaybeSchemaTransformer>();
        options.AddSchemaTransformer<IntervalSchemaTransformer>();
        options.AddSchemaTransformer<StrongTypeCollectionShapeTransformer>();
        options.AddOperationTransformer<NonBodyStrongTypeOperationTransformer>();
        options.AddDocumentTransformer<StrongTypesComponentSchemaFiller>();
        options.AddDocumentTransformer<PropertyAnnotationSchemaTransformer>();
        options.AddDocumentTransformer<StrongTypeInliningDocumentTransformer>();
        return options;
    }
}
