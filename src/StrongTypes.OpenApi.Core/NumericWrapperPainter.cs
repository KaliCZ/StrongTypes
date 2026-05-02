using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Paints a schema as the underlying primitive of a numeric strong-type
/// wrapper, plus its single bound. Shared by the Microsoft and Swashbuckle
/// schema-time painters and by the document-time component filler so all
/// three pipelines emit the same wire shape.
/// </summary>
public static class NumericWrapperPainter
{
    public static void Paint(OpenApiSchema schema, Type underlying, NumericBound bound)
    {
        var info = PrimitiveSchemaMap.TryGet(underlying, out var i) ? i : new PrimitiveSchemaMap.Info(JsonSchemaType.Number, null);
        Paint(schema, info, bound);
    }

    public static void Paint(OpenApiSchema schema, PrimitiveSchemaMap.Info info, NumericBound bound)
    {
        SchemaPaint.ClearWrapperShape(schema);
        schema.Type = info.Type;
        schema.Format = info.Format;

        if (bound.IsLower)
            SchemaPaint.TightenLowerBound(schema, bound.Value, bound.Exclusive);
        else
            SchemaPaint.TightenUpperBound(schema, bound.Value, bound.Exclusive);
    }
}
