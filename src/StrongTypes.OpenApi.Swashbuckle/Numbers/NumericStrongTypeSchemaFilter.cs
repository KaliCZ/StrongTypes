using System.Globalization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>Rewrites the schema for the numeric strong-type wrappers <see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>, <see cref="Negative{T}"/>, and <see cref="NonPositive{T}"/> to the underlying primitive type with the appropriate minimum/maximum bound.</summary>
public sealed class NumericStrongTypeSchemaFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, (JsonSchemaType Type, string? Format)> s_primitiveMap = new()
    {
        [typeof(sbyte)] = (JsonSchemaType.Integer, "int32"),
        [typeof(byte)] = (JsonSchemaType.Integer, "int32"),
        [typeof(short)] = (JsonSchemaType.Integer, "int32"),
        [typeof(ushort)] = (JsonSchemaType.Integer, "int32"),
        [typeof(int)] = (JsonSchemaType.Integer, "int32"),
        [typeof(uint)] = (JsonSchemaType.Integer, "int64"),
        [typeof(long)] = (JsonSchemaType.Integer, "int64"),
        [typeof(ulong)] = (JsonSchemaType.Integer, "int64"),
        [typeof(float)] = (JsonSchemaType.Number, "float"),
        [typeof(double)] = (JsonSchemaType.Number, "double"),
        [typeof(decimal)] = (JsonSchemaType.Number, "double"),
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsGenericType) return;
        if (schema is not OpenApiSchema concrete) return;

        var definition = type.GetGenericTypeDefinition();
        var underlying = type.GetGenericArguments()[0];

        if (definition == typeof(Positive<>))
            Rewrite(concrete, underlying, minimum: 0m, exclusiveMinimum: true);
        else if (definition == typeof(NonNegative<>))
            Rewrite(concrete, underlying, minimum: 0m, exclusiveMinimum: false);
        else if (definition == typeof(Negative<>))
            Rewrite(concrete, underlying, maximum: 0m, exclusiveMaximum: true);
        else if (definition == typeof(NonPositive<>))
            Rewrite(concrete, underlying, maximum: 0m, exclusiveMaximum: false);
    }

    private static void Rewrite(
        OpenApiSchema schema,
        Type underlying,
        decimal? minimum = null,
        bool exclusiveMinimum = false,
        decimal? maximum = null,
        bool exclusiveMaximum = false)
    {
        StrongTypesSchemaReset.ResetToScalar(schema);

        if (s_primitiveMap.TryGetValue(underlying, out var map))
        {
            schema.Type = map.Type;
            schema.Format = map.Format;
        }
        else
        {
            schema.Type = JsonSchemaType.Number;
        }

        if (minimum is { } min)
        {
            var text = min.ToString(CultureInfo.InvariantCulture);
            if (exclusiveMinimum) schema.ExclusiveMinimum = text;
            else schema.Minimum = text;
        }

        if (maximum is { } max)
        {
            var text = max.ToString(CultureInfo.InvariantCulture);
            if (exclusiveMaximum) schema.ExclusiveMaximum = text;
            else schema.Maximum = text;
        }
    }
}
