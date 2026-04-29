using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>Rewrites the schema for the numeric strong-type wrappers <see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>, <see cref="Negative{T}"/>, and <see cref="NonPositive{T}"/> to the underlying primitive type with the appropriate minimum/maximum floor. Caller-supplied <c>[Range]</c> annotations are preserved when at least as tight as the wrapper's floor.</summary>
public sealed class NumericStrongTypeSchemaTransformer : IOpenApiSchemaTransformer
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

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsGenericType) return Task.CompletedTask;

        var definition = type.GetGenericTypeDefinition();
        var underlying = type.GetGenericArguments()[0];

        if (definition == typeof(Positive<>))
            Paint(schema, underlying, lowerFloor: (0m, true));
        else if (definition == typeof(NonNegative<>))
            Paint(schema, underlying, lowerFloor: (0m, false));
        else if (definition == typeof(Negative<>))
            Paint(schema, underlying, upperFloor: (0m, true));
        else if (definition == typeof(NonPositive<>))
            Paint(schema, underlying, upperFloor: (0m, false));

        return Task.CompletedTask;
    }

    private static void Paint(
        OpenApiSchema schema,
        Type underlying,
        (decimal Value, bool Exclusive)? lowerFloor = null,
        (decimal Value, bool Exclusive)? upperFloor = null)
    {
        SchemaPaint.ClearWrapperShape(schema);

        if (s_primitiveMap.TryGetValue(underlying, out var map))
        {
            schema.Type = map.Type;
            schema.Format = map.Format;
        }
        else
        {
            schema.Type = JsonSchemaType.Number;
        }

        if (lowerFloor is { } lower)
            SchemaPaint.TightenLowerBound(schema, lower.Value, lower.Exclusive);
        if (upperFloor is { } upper)
            SchemaPaint.TightenUpperBound(schema, upper.Value, upper.Exclusive);
    }
}
