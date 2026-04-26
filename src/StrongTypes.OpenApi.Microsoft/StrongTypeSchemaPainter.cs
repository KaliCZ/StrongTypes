using System.Collections.Generic;
using System.Globalization;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// Reverses a strong-type-derived schema name (e.g. "MaybeOfPositiveOfint",
// "NonEmptyEnumerableOfNonEmptyString") back into the wire schema the
// converter actually emits, then paints it onto the supplied schema. Used by
// the document transformer to repopulate component schemas the framework
// emptied during its deduplication pass.
internal static class StrongTypeSchemaPainter
{
    private static readonly Dictionary<string, (JsonSchemaType Type, string? Format)> s_primitives = new()
    {
        ["sbyte"] = (JsonSchemaType.Integer, "int32"),
        ["byte"] = (JsonSchemaType.Integer, "int32"),
        ["short"] = (JsonSchemaType.Integer, "int32"),
        ["ushort"] = (JsonSchemaType.Integer, "int32"),
        ["int"] = (JsonSchemaType.Integer, "int32"),
        ["uint"] = (JsonSchemaType.Integer, "int64"),
        ["long"] = (JsonSchemaType.Integer, "int64"),
        ["ulong"] = (JsonSchemaType.Integer, "int64"),
        ["float"] = (JsonSchemaType.Number, "float"),
        ["double"] = (JsonSchemaType.Number, "double"),
        ["decimal"] = (JsonSchemaType.Number, "double"),
        ["string"] = (JsonSchemaType.String, null),
        ["bool"] = (JsonSchemaType.Boolean, null),
    };

    public static bool TryPaint(string name, OpenApiSchema schema, IDictionary<string, IOpenApiSchema> components)
    {
        var painted = TryBuild(name, components);
        if (painted is null) return false;

        StrongTypesSchemaReset.ResetToScalar(schema);
        CopyInto(schema, painted);
        return true;
    }

    private static OpenApiSchema? TryBuild(string name, IDictionary<string, IOpenApiSchema> components)
    {
        if (name == "NonEmptyString")
            return new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 };

        if (TrySplit(name, "PositiveOf", out var inner))
            return Numeric(inner, minimum: 0m, exclusiveMinimum: true);
        if (TrySplit(name, "NonNegativeOf", out inner))
            return Numeric(inner, minimum: 0m, exclusiveMinimum: false);
        if (TrySplit(name, "NegativeOf", out inner))
            return Numeric(inner, maximum: 0m, exclusiveMaximum: true);
        if (TrySplit(name, "NonPositiveOf", out inner))
            return Numeric(inner, maximum: 0m, exclusiveMaximum: false);

        if (TrySplit(name, "NonEmptyEnumerableOf", out inner))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                MinItems = 1,
                Items = ResolveInner(inner, components),
            };
        }

        if (TrySplit(name, "MaybeOf", out inner))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["Value"] = ResolveInner(inner, components),
                },
            };
        }

        return null;
    }

    private static IOpenApiSchema ResolveInner(string innerName, IDictionary<string, IOpenApiSchema> components)
    {
        if (s_primitives.TryGetValue(innerName, out var prim))
        {
            return new OpenApiSchema { Type = prim.Type, Format = prim.Format };
        }

        var built = TryBuild(innerName, components);
        if (built is not null) return built;

        // Fall back to a $ref so unfamiliar type names still produce a valid
        // schema (and the framework's own component will supply the body).
        return new OpenApiSchemaReference(innerName);
    }

    private static OpenApiSchema Numeric(string inner, decimal? minimum = null, bool exclusiveMinimum = false, decimal? maximum = null, bool exclusiveMaximum = false)
    {
        var (type, format) = s_primitives.TryGetValue(inner, out var p) ? p : (JsonSchemaType.Number, (string?)null);
        var schema = new OpenApiSchema { Type = type, Format = format };

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

        return schema;
    }

    private static bool TrySplit(string name, string prefix, out string rest)
    {
        if (name.StartsWith(prefix))
        {
            rest = name[prefix.Length..];
            return true;
        }

        rest = string.Empty;
        return false;
    }

    private static void CopyInto(OpenApiSchema target, OpenApiSchema source)
    {
        target.Type = source.Type;
        target.Format = source.Format;
        target.MinLength = source.MinLength;
        target.MaxLength = source.MaxLength;
        target.MinItems = source.MinItems;
        target.MaxItems = source.MaxItems;
        target.Minimum = source.Minimum;
        target.Maximum = source.Maximum;
        target.ExclusiveMinimum = source.ExclusiveMinimum;
        target.ExclusiveMaximum = source.ExclusiveMaximum;
        target.Pattern = source.Pattern;
        target.Items = source.Items;
        target.Properties = source.Properties;
    }
}
