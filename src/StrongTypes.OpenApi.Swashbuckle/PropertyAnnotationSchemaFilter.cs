using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Re-applies caller-supplied data-annotations to properties whose CLR type is a strong-type
/// wrapper: such a property renders as a bare <c>$ref</c>, which silently drops the caller's
/// bounds, so they are layered onto the property position via <c>allOf</c>. The same wrapper
/// also records a nullable member's null bit, which Swashbuckle likewise drops from a bare
/// <c>$ref</c>.
/// </summary>
public sealed class PropertyAnnotationSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;
        if (concrete.Properties is null || concrete.Properties.Count == 0) return;
        if (context.MemberInfo is not null || context.ParameterInfo is not null) return;

        var nullability = new NullabilityInfoContext();
        var clrProperties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var clrProperty in clrProperties)
        {
            var jsonName = ResolveJsonName(clrProperty);
            if (!concrete.Properties.TryGetValue(jsonName, out var propSchema)) continue;

            var surrogate = StrongTypeSchemaTypes.ResolveWireType(clrProperty.PropertyType);
            if (surrogate is null) continue;

            var isNullable = IsNullableMember(clrProperty, nullability);

            // Only act when there is something to layer on; otherwise leave the bare $ref for the inliner.
            OpenApiSchema? source = null;
            if (clrProperty.GetCustomAttributes(inherit: true).OfType<Attribute>().Any())
                source = context.SchemaGenerator.GenerateSchema(surrogate, context.SchemaRepository, memberInfo: clrProperty) as OpenApiSchema;

            if (source is null && !isNullable) continue;

            if (propSchema is OpenApiSchema inline && inline.Type is not null)
            {
                if (source is not null) CopyAnnotationKeywords(source, inline);
                if (isNullable) SchemaPaint.MarkNullable(inline);
                continue;
            }

            var wrapper = new OpenApiSchema { AllOf = [propSchema] };
            if (source is not null) CopyAnnotationKeywords(source, wrapper);
            if (isNullable) SchemaPaint.MarkNullable(wrapper);
            StrongTypeInlineMarker.Set(wrapper);
            concrete.Properties[jsonName] = wrapper;
        }
    }

    private static bool IsNullableMember(PropertyInfo property, NullabilityInfoContext context)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null) return true;
        if (property.PropertyType.IsValueType) return false;
        return context.Create(property).ReadState == NullabilityState.Nullable;
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
