using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Re-applies caller-supplied data-annotations (<c>[StringLength]</c>,
/// <c>[Range]</c>, <c>[RegularExpression]</c>, <c>[Description]</c>, …)
/// to properties whose CLR type is a strong-type wrapper. Without this
/// pass, a property like <c>[StringLength(50)] NonEmptyString Name</c>
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
/// painted by <see cref="NonEmptyStringSchemaTransformer"/> and is not
/// touched here — only the property position is. After
/// <see cref="StrongTypeInliner"/> collapses the <c>allOf+ref</c>, the
/// merged shape on the wire is
/// <c>{ type: string, minLength: 1, maxLength: 50 }</c>. Also normalises
/// each parent schema's <c>required</c> set to the C# nullability of its
/// properties.
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

            // OpenApiSchemaReference (or any other IOpenApiSchema implementation):
            // wrap it so we can layer the bounds on top.
            var refWrapper = new OpenApiSchema { AllOf = [propSchema] };
            if (WrapperAnnotationApplier.TryApply(refWrapper, clrProperty.PropertyType, attrs))
            {
                StrongTypeInlineMarker.Set(refWrapper);
                parent.Properties[jsonName] = refWrapper;
            }
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

        var schemaName = ComputeSchemaName(type);
        if (!map.ContainsKey(schemaName)) map[schemaName] = type;

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Visit(p.PropertyType, map, seen);
    }

    // Mimics Microsoft.AspNetCore.OpenApi's default schema-id strategy:
    //   `Foo`            → "Foo"
    //   `Foo<Bar>`       → "FooOfBar"
    //   `Foo<Bar<Baz>>`  → "FooOfBarOfBaz"
    //   primitive types  → C# keyword (int, string, …) — that's what shows
    //                      up in component names like "PositiveOfint".
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
