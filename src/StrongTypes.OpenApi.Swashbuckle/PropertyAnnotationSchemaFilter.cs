using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Re-applies caller-supplied data-annotations (<c>[StringLength]</c>,
/// <c>[Range]</c>, <c>[RegularExpression]</c>, <c>[Description]</c>, …)
/// to properties whose CLR type is a strong-type wrapper. Without this
/// filter, a property like <c>[StringLength(50)] NonEmptyString Name</c>
/// renders as a bare <c>$ref</c> to the <c>NonEmptyString</c> component
/// and silently drops the caller's <c>maxLength: 50</c>. We layer the
/// caller's bound onto the property position via <c>allOf</c>:
/// <code>
/// "name": {
///   "allOf": [ { "$ref": "#/components/schemas/NonEmptyString" } ],
///   "maxLength": 50
/// }
/// </code>
/// The component schema (<c>{ "type": "string", "minLength": 1 }</c>) is
/// painted by <see cref="NonEmptyStringSchemaFilter"/> and is not touched
/// here — only the property position is. After
/// <see cref="StrongTypeInliner"/> collapses the <c>allOf+ref</c>, the
/// merged shape on the wire is <c>{ type: string, minLength: 1, maxLength: 50 }</c>.
/// The wrapper-typed surface matches whatever Swashbuckle natively supports
/// for the equivalent primitive-typed property — no more, no less.
/// </summary>
public sealed class PropertyAnnotationSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;
        if (concrete.Properties is null || concrete.Properties.Count == 0) return;
        if (context.MemberInfo is not null || context.ParameterInfo is not null) return;

        var clrProperties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var clrProperty in clrProperties)
        {
            var jsonName = ResolveJsonName(clrProperty);
            if (!concrete.Properties.TryGetValue(jsonName, out var propSchema)) continue;

            var attrs = clrProperty.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
            if (attrs.Length == 0) continue;

            var surrogate = StrongTypeSchemaTypes.ResolveAnnotationSurrogate(clrProperty.PropertyType);
            if (surrogate is null) continue;

            var generated = context.SchemaGenerator.GenerateSchema(surrogate, context.SchemaRepository, memberInfo: clrProperty);
            if (generated is not OpenApiSchema source) continue;

            if (propSchema is OpenApiSchema inline && inline.Type is not null)
            {
                CopyAnnotationKeywords(source, inline);
                continue;
            }

            var wrapper = new OpenApiSchema { AllOf = [propSchema] };
            CopyAnnotationKeywords(source, wrapper);
            StrongTypeInlineMarker.Set(wrapper);
            concrete.Properties[jsonName] = wrapper;
        }
    }

    private static void CopyAnnotationKeywords(OpenApiSchema source, OpenApiSchema target)
    {
        if (source.MinLength is { } minL) SchemaPaint.TightenMinLength(target, minL);
        if (source.MaxLength is { } maxL) SchemaPaint.TightenMaxLength(target, maxL);
        if (source.MinItems is { } minI) SchemaPaint.TightenMinItems(target, minI);
        if (source.MaxItems is { } maxI) SchemaPaint.TightenMaxItems(target, maxI);
        if (!string.IsNullOrEmpty(source.Pattern)) SchemaPaint.SetPatternIfAbsent(target, source.Pattern);
        if (!string.IsNullOrEmpty(source.Format)) SchemaPaint.SetFormatIfAbsent(target, source.Format);
        if (!string.IsNullOrEmpty(source.Description)) SchemaPaint.SetDescriptionIfAbsent(target, source.Description);
        if (source.Default is not null) SchemaPaint.SetDefaultIfAbsent(target, source.Default);

        if (TryParseDecimal(source.Minimum, out var min))
            SchemaPaint.TightenLowerBound(target, min, floorExclusive: false);
        if (TryParseDecimal(source.ExclusiveMinimum, out var exMin))
            SchemaPaint.TightenLowerBound(target, exMin, floorExclusive: true);
        if (TryParseDecimal(source.Maximum, out var max))
            SchemaPaint.TightenUpperBound(target, max, floorExclusive: false);
        if (TryParseDecimal(source.ExclusiveMaximum, out var exMax))
            SchemaPaint.TightenUpperBound(target, exMax, floorExclusive: true);
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        if (value is not null && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return true;
        result = 0m;
        return false;
    }

    private static string ResolveJsonName(PropertyInfo property)
    {
        var jsonNameAttr = property.GetCustomAttributes(inherit: true)
            .OfType<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
            .FirstOrDefault();
        if (jsonNameAttr is not null) return jsonNameAttr.Name;

        return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}
