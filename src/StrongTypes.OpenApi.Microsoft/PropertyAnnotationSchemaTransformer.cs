using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Re-applies caller-supplied data-annotations to properties whose CLR type is a strong-type
/// wrapper: such a property renders as a bare <c>$ref</c>, which silently drops the caller's
/// bounds, so they are layered onto the property position via <c>allOf</c>.
/// </summary>
internal sealed class PropertyAnnotationSchemaTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is not { } schemas) return Task.CompletedTask;

        var componentNameToType = BuildComponentToTypeMap(context.DescriptionGroups);

        foreach (var (componentName, componentSchema) in schemas)
        {
            if (componentSchema is not OpenApiSchema parent) continue;
            if (parent.Properties is null || parent.Properties.Count == 0) continue;
            if (!componentNameToType.TryGetValue(componentName, out var parentType)) continue;

            ApplyToParent(parent, parentType);
        }

        return Task.CompletedTask;
    }

    private static void ApplyToParent(OpenApiSchema parent, Type parentType)
    {
        var clrProperties = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var clrProperty in clrProperties)
        {
            var jsonName = ResolveJsonName(clrProperty);
            if (!parent.Properties!.TryGetValue(jsonName, out var propSchema)) continue;

            var attrs = clrProperty.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
            if (attrs.Length == 0) continue;

            if (propSchema is OpenApiSchema inline && IsBareRefSchema(inline))
            {
                var wrapper = new OpenApiSchema { AllOf = [propSchema] };
                if (WrapperAnnotationApplier.TryApply(wrapper, clrProperty.PropertyType, attrs))
                {
                    StrongTypeInlineMarker.Set(wrapper);
                    parent.Properties[jsonName] = wrapper;
                }
                continue;
            }

            if (propSchema is OpenApiSchema concrete)
            {
                WrapperAnnotationApplier.TryApply(concrete, clrProperty.PropertyType, attrs);
                continue;
            }

            var refWrapper = new OpenApiSchema { AllOf = [propSchema] };
            if (WrapperAnnotationApplier.TryApply(refWrapper, clrProperty.PropertyType, attrs))
            {
                StrongTypeInlineMarker.Set(refWrapper);
                parent.Properties[jsonName] = refWrapper;
            }
        }
    }

    // The pipeline renders a bare $ref property position as an otherwise-empty concrete schema.
    private static bool IsBareRefSchema(OpenApiSchema schema)
    {
        if (schema.Type is not null) return false;
        if (schema.Properties is { Count: > 0 }) return false;
        if (schema.AllOf is { Count: > 0 }) return false;
        if (schema.OneOf is { Count: > 0 }) return false;
        if (schema.AnyOf is { Count: > 0 }) return false;
        if (schema.Items is not null) return false;
        return true;
    }

    private static string ResolveJsonName(PropertyInfo property)
    {
        var jsonNameAttr = property.GetCustomAttributes(inherit: true)
            .OfType<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
            .FirstOrDefault();
        if (jsonNameAttr is not null) return jsonNameAttr.Name;

        return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }

    private static Dictionary<string, Type> BuildComponentToTypeMap(IReadOnlyList<ApiDescriptionGroup> groups)
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        var seen = new HashSet<Type>();

        foreach (var group in groups)
        {
            foreach (var description in group.Items)
            {
                foreach (var parameter in description.ParameterDescriptions)
                    Visit(parameter.Type, map, seen);
                foreach (var response in description.SupportedResponseTypes)
                    Visit(response.Type, map, seen);
            }
        }

        return map;
    }

    private static void Visit(Type? type, Dictionary<string, Type> map, HashSet<Type> seen)
    {
        if (type is null || !seen.Add(type)) return;

        var schemaName = ComputeSchemaName(type);
        if (!map.ContainsKey(schemaName)) map[schemaName] = type;

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Visit(p.PropertyType, map, seen);
    }

    // Mimics Microsoft.AspNetCore.OpenApi's default schema-id strategy (Foo<Bar> → "FooOfBar", primitives → C# keyword).
    private static string ComputeSchemaName(Type type)
    {
        if (MicrosoftSchemaNaming.GetPrimitiveKeyword(type) is { } keyword) return keyword;

        if (!type.IsGenericType) return type.Name;

        var raw = type.Name;
        var backtick = raw.IndexOf('`');
        var baseName = backtick < 0 ? raw : raw[..backtick];

        var args = type.GetGenericArguments();
        var argNames = string.Concat(args.Select(ComputeSchemaName));
        return $"{baseName}Of{argNames}";
    }
}
