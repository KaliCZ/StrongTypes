using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Entry point for wiring StrongTypes schema filters into Swashbuckle's
/// <see cref="SwaggerGenOptions"/>.
/// </summary>
public static class StrongTypesSwashbuckleExtensions
{
    /// <summary>
    /// Registers schema filters so <see cref="NonEmptyString"/>, the
    /// numeric strong-type wrappers, <see cref="NonEmptyEnumerable{T}"/> /
    /// <see cref="INonEmptyEnumerable{T}"/>, <see cref="Maybe{T}"/>, and the
    /// interval types (<see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>,
    /// <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>) render in
    /// the generated Swagger document as the JSON shape their converters
    /// actually produce.
    /// </summary>
    /// <param name="options">The Swagger generator options being configured.</param>
    public static SwaggerGenOptions AddStrongTypes(this SwaggerGenOptions options)
    {
        options.SchemaFilter<NonEmptyStringSchemaFilter>();
        options.SchemaFilter<EmailSchemaFilter>();
        options.SchemaFilter<DigitSchemaFilter>();
        options.SchemaFilter<NumericStrongTypeSchemaFilter>();
        options.SchemaFilter<NonEmptyEnumerableSchemaFilter>();
        options.SchemaFilter<MaybeSchemaFilter>();
        options.SchemaFilter<IntervalSchemaFilter>();
        options.SchemaFilter<PropertyAnnotationSchemaFilter>();
        options.OperationFilter<NonBodyStrongTypeOperationFilter>();
        options.DocumentFilter<StrongTypeInliningDocumentFilter>();
        return options;
    }
}
