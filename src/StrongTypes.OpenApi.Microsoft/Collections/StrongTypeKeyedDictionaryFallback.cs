using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// Fallback for one specific gap in Microsoft.AspNetCore.OpenApi: when a
// dictionary's *key* is a strong type (e.g. `Dictionary<NonEmptyString,
// int>`), the framework inlines a broken `additionalProperties` schema on
// the parent record's property AND never invokes schema-level transformers
// on the dictionary type — so StrongTypeCollectionShapeTransformer never
// gets a chance to fix it. Every other dictionary shape — including
// strong-typed *values* — flows through the schema-transformer hook
// normally and does not need this fallback. We patch the broken position
// here at document level by walking parent components and rebuilding
// `additionalProperties` from the CLR value type.
internal sealed class StrongTypeKeyedDictionaryFallback : IOpenApiDocumentTransformer
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

            ApplyToParent(parent, parentType, schemas);
        }

        return Task.CompletedTask;
    }

    private static void ApplyToParent(OpenApiSchema parent, Type parentType, IDictionary<string, IOpenApiSchema> components)
    {
        foreach (var clrProperty in parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (TryGetDictionaryValueType(clrProperty.PropertyType) is not { } valueType) continue;

            var jsonName = ResolveJsonName(clrProperty);
            if (!parent.Properties!.TryGetValue(jsonName, out var propSchema)) continue;
            if (propSchema is not OpenApiSchema inline) continue;

            inline.Type = JsonSchemaType.Object;
            inline.AdditionalProperties = BuildValueSchema(valueType, components);
        }
    }

    private static IOpenApiSchema BuildValueSchema(Type valueType, IDictionary<string, IOpenApiSchema> components)
    {
        if (TryBuildPrimitiveSchema(valueType) is { } primitive) return primitive;

        var name = ComputeSchemaName(valueType);
        if (components.ContainsKey(name)) return new OpenApiSchemaReference(name);

        return new OpenApiSchema();
    }

    private static OpenApiSchema? TryBuildPrimitiveSchema(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(byte) || underlying == typeof(sbyte) || underlying == typeof(short) || underlying == typeof(ushort) || underlying == typeof(int))
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        if (underlying == typeof(uint) || underlying == typeof(long) || underlying == typeof(ulong))
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
        if (underlying == typeof(float))
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
        if (underlying == typeof(double) || underlying == typeof(decimal))
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" };
        if (underlying == typeof(bool))
            return new OpenApiSchema { Type = JsonSchemaType.Boolean };
        if (underlying == typeof(string))
            return new OpenApiSchema { Type = JsonSchemaType.String };
        if (underlying == typeof(Guid))
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        return null;
    }

    private static Type? TryGetDictionaryValueType(Type type)
    {
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                return type.GetGenericArguments()[1];
        }
        foreach (var face in type.GetInterfaces())
        {
            if (!face.IsGenericType) continue;
            var def = face.GetGenericTypeDefinition();
            if (def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                return face.GetGenericArguments()[1];
        }
        return null;
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

    private static string ComputeSchemaName(Type type)
    {
        var keyword = GetPrimitiveKeyword(type);
        if (keyword is not null) return keyword;

        if (!type.IsGenericType) return type.Name;

        var raw = type.Name;
        var backtick = raw.IndexOf('`');
        var baseName = backtick < 0 ? raw : raw[..backtick];

        var args = type.GetGenericArguments();
        var argNames = string.Concat(args.Select(ComputeSchemaName));
        return $"{baseName}Of{argNames}";
    }

    private static string? GetPrimitiveKeyword(Type type)
    {
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(char)) return "char";
        return null;
    }
}
