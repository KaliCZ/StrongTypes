using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Re-applies caller-supplied data-annotations (<c>[StringLength]</c>,
/// <c>[RegularExpression]</c>, <c>[Range]</c>, <c>[MaxLength]</c>,
/// <c>[MinLength]</c>) to property positions whose CLR type is a strong-type
/// wrapper. Without this pass the Microsoft pipeline would render the
/// property as a bare <c>$ref</c> to the wrapper component and silently
/// drop every caller annotation — the very contract this package exists
/// to preserve.
///
/// Implemented as a <see cref="IOpenApiDocumentTransformer"/> rather than a
/// schema transformer because the framework's deduplication pass rewrites
/// inline property schemas back into <c>$ref</c>s based on CLR type
/// equality. By the time this transformer runs (document-level, after all
/// schema-level transforms and the dedup pass), the parent-schema property
/// entries we want to rewrite are stable.
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
                    parent.Properties[jsonName] = wrapper;
                continue;
            }

            if (propSchema is OpenApiSchema concrete)
            {
                WrapperAnnotationApplier.TryApply(concrete, clrProperty.PropertyType, attrs);
                continue;
            }

            // OpenApiSchemaReference (or any other IOpenApiSchema implementation):
            // wrap it so we can layer the bounds on top.
            var refWrapper = new OpenApiSchema { AllOf = [propSchema] };
            if (WrapperAnnotationApplier.TryApply(refWrapper, clrProperty.PropertyType, attrs))
                parent.Properties[jsonName] = refWrapper;
        }
    }

    // A "bare ref" position emerges as an OpenApiSchema whose only meaningful
    // signal is the Reference pointer. Wrapping in `allOf:[ref]` is the
    // standard way to layer caller bounds on top of it.
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
        // Microsoft's default schema-id strategy is the simple CLR type name (or
        // a generic-sanitised form). Map component names to CLR types by
        // walking every reachable parameter / return type the API exposes.
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

        var schemaName = type.Name;
        if (!map.ContainsKey(schemaName)) map[schemaName] = type;

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Visit(p.PropertyType, map, seen);
    }
}
