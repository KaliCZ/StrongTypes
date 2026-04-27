using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Re-applies caller-supplied data-annotations (<c>[StringLength]</c>,
/// <c>[RegularExpression]</c>, <c>[Range]</c>, <c>[MaxLength]</c>,
/// <c>[MinLength]</c>) to property positions whose CLR type is a strong-type
/// wrapper. Without this filter Swashbuckle renders the property as a bare
/// <c>$ref</c> to the wrapper component and silently drops every caller
/// annotation.
///
/// Runs over each parent schema (record / class with <c>properties</c>): it
/// walks the parent type's <see cref="PropertyInfo"/> set, looks up the
/// matching schema property by JSON name, and rewrites the existing
/// <c>$ref</c> position into <c>{ allOf: [$ref], &lt;bounds&gt; }</c> when
/// annotations are present on the property.
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

            if (propSchema is OpenApiSchema inline)
            {
                WrapperAnnotationApplier.TryApply(inline, clrProperty.PropertyType, attrs);
                continue;
            }

            var wrapper = new OpenApiSchema { AllOf = [propSchema] };
            if (WrapperAnnotationApplier.TryApply(wrapper, clrProperty.PropertyType, attrs))
                concrete.Properties[jsonName] = wrapper;
        }
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
