using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Re-applies caller-supplied data-annotations to property positions whose CLR
/// type is a strong-type wrapper. Without this filter Swashbuckle renders the
/// property as a bare <c>$ref</c> to the wrapper component and silently drops
/// every caller annotation.
///
/// Strategy: for each wrapper-typed property carrying attributes, ask
/// Swashbuckle's own <see cref="ISchemaGenerator"/> to generate a schema for
/// the wrapper's surrogate primitive type with the property's
/// <see cref="MemberInfo"/> attached. That call re-runs Swashbuckle's filter
/// chain (including <c>DataAnnotationsSchemaFilter</c> and any third-party
/// filters the host has registered), so every keyword Swashbuckle natively
/// supports for primitive-typed properties also surfaces here. We then copy
/// the resulting keywords onto our wrapper position via <see cref="SchemaPaint"/>'s
/// tighten/set-if-absent helpers, so the wrapper's wire-shape floor still wins.
/// Attributes Swashbuckle doesn't natively handle on primitive-typed
/// properties stay unsupported here too — the wrapper-typed surface
/// matches the primitive-typed surface, by design.
/// </summary>
public sealed class PropertyAnnotationSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;
        if (concrete.Properties is null || concrete.Properties.Count == 0) return;
        if (context.MemberInfo is not null || context.ParameterInfo is not null) return;

        NormaliseRequired(concrete, context.Type);

        var clrProperties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var clrProperty in clrProperties)
        {
            var jsonName = ResolveJsonName(clrProperty);
            if (!concrete.Properties.TryGetValue(jsonName, out var propSchema)) continue;

            var attrs = clrProperty.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
            if (attrs.Length == 0) continue;

            var surrogate = ResolveSurrogateType(clrProperty.PropertyType);
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
            concrete.Properties[jsonName] = wrapper;
        }
    }

    private static Type? ResolveSurrogateType(Type propertyClrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(propertyClrType) ?? propertyClrType;
        if (unwrapped == typeof(NonEmptyString)) return typeof(string);
        if (!unwrapped.IsGenericType) return null;

        var def = unwrapped.GetGenericTypeDefinition();
        var arg = unwrapped.GetGenericArguments()[0];

        if (def == typeof(Positive<>) || def == typeof(NonNegative<>) ||
            def == typeof(Negative<>) || def == typeof(NonPositive<>))
            return arg;

        if (def == typeof(NonEmptyEnumerable<>) || def == typeof(INonEmptyEnumerable<>))
            return typeof(IEnumerable<>).MakeGenericType(arg);

        return null;
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

    private static void NormaliseRequired(OpenApiSchema parent, Type parentType)
    {
        var required = RequiredSet.ComputeJsonNames(parentType);
        if (parent.Properties is { } props)
            required.IntersectWith(props.Keys);
        parent.Required = required;
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
