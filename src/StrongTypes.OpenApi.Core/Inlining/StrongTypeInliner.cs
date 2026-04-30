using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Walks an <see cref="OpenApiDocument"/> and replaces every <c>$ref</c> to
/// a strong-type wrapper component whose wire shape is a primitive or an
/// array (<c>NonEmptyString</c>, <c>PositiveOf*</c>, <c>NonNegativeOf*</c>,
/// <c>NegativeOf*</c>, <c>NonPositiveOf*</c>, <c>NonEmptyEnumerableOf*</c>,
/// plus Swashbuckle's suffix-style equivalents) with the wrapper's wire
/// body, merging any caller-supplied annotations attached at the use site
/// (the <c>allOf:[ref]</c> + <c>maxLength</c>/<c>maxItems</c>/etc. shape
/// that <see cref="SchemaPaint"/>'s tighten helpers produce). After all
/// references are inlined, the wrapper components themselves are dropped
/// from <c>components.schemas</c>.
/// <para>
/// <see cref="Maybe{T}"/> is <em>not</em> inlined — its wire shape is an
/// object with a <c>Value</c> property, which has a real type identity
/// worth keeping as a named component.
/// </para>
/// </summary>
public static class StrongTypeInliner
{
    public static void Inline(OpenApiDocument document)
    {
        if (document.Components?.Schemas is not { } schemas) return;

        var inlineable = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        foreach (var (name, schema) in schemas)
        {
            if (schema is OpenApiSchema concrete
                && IsInlineableName(name)
                && IsInlineableShape(concrete))
                inlineable[name] = concrete;
        }
        if (inlineable.Count == 0) return;

        // Walk every component (including the inlineable bodies themselves)
        // so refs to other inlineable wrappers nested inside an array
        // wrapper's `items` get resolved before we use the wrapper as a
        // template at use sites.
        foreach (var (_, schema) in schemas)
        {
            if (schema is OpenApiSchema concrete) RewriteSchema(concrete, inlineable);
        }

        if (document.Paths is not null)
        {
            foreach (var (_, pathItem) in document.Paths)
                RewritePathItem(pathItem, inlineable);
        }

        foreach (var name in inlineable.Keys)
            schemas.Remove(name);
    }

    private static bool IsInlineableName(string name)
    {
        if (string.Equals(name, "NonEmptyString", StringComparison.Ordinal)) return true;

        // Microsoft.AspNetCore.OpenApi: PositiveOfint, NonEmptyEnumerableOfNonEmptyString, …
        if (name.StartsWith("PositiveOf", StringComparison.Ordinal)
            || name.StartsWith("NonNegativeOf", StringComparison.Ordinal)
            || name.StartsWith("NegativeOf", StringComparison.Ordinal)
            || name.StartsWith("NonPositiveOf", StringComparison.Ordinal)
            || name.StartsWith("NonEmptyEnumerableOf", StringComparison.Ordinal))
            return true;

        // Swashbuckle suffix style: Int32Positive, Int64NonNegative, DoubleNegative,
        // DecimalNonPositive, Int32NonEmptyEnumerable, …
        return name.EndsWith("Positive", StringComparison.Ordinal)
            || name.EndsWith("Negative", StringComparison.Ordinal)
            || name.EndsWith("NonNegative", StringComparison.Ordinal)
            || name.EndsWith("NonPositive", StringComparison.Ordinal)
            || name.EndsWith("NonEmptyEnumerable", StringComparison.Ordinal);
    }

    private static bool IsInlineableShape(OpenApiSchema schema)
    {
        if (schema.Properties is { Count: > 0 }) return false;
        if (schema.AdditionalProperties is not null) return false;
        if (schema.AllOf is { Count: > 0 }) return false;
        if (schema.OneOf is { Count: > 0 }) return false;
        if (schema.AnyOf is { Count: > 0 }) return false;

        if (schema.Type == JsonSchemaType.Array)
            return schema.Items is not null;

        return schema.Type == JsonSchemaType.String
            || schema.Type == JsonSchemaType.Integer
            || schema.Type == JsonSchemaType.Number;
    }

    private static void RewritePathItem(IOpenApiPathItem pathItem, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (pathItem.Operations is { } operations)
        {
            foreach (var (_, operation) in operations)
                RewriteOperation(operation, inlineable);
        }

        if (pathItem.Parameters is { } pathParams)
        {
            foreach (var p in pathParams)
                RewriteParameter(p, inlineable);
        }
    }

    private static void RewriteOperation(OpenApiOperation operation, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (operation.Parameters is { } parameters)
        {
            foreach (var p in parameters)
                RewriteParameter(p, inlineable);
        }

        if (operation.RequestBody is { Content: { } reqContent })
        {
            foreach (var (_, media) in reqContent)
                RewriteMediaType(media, inlineable);
        }

        if (operation.Responses is { } responses)
        {
            foreach (var (_, response) in responses)
            {
                if (response.Content is { } respContent)
                {
                    foreach (var (_, media) in respContent)
                        RewriteMediaType(media, inlineable);
                }
                if (response.Headers is { } headers)
                {
                    foreach (var (_, header) in headers)
                    {
                        if (header is OpenApiHeader concreteHeader && concreteHeader.Schema is { } headerSchema)
                            concreteHeader.Schema = RewriteSlot(headerSchema, inlineable);
                    }
                }
            }
        }
    }

    private static void RewriteParameter(IOpenApiParameter parameter, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (parameter is OpenApiParameter concrete && concrete.Schema is { } schema)
            concrete.Schema = RewriteSlot(schema, inlineable);

        if (parameter.Content is { } content)
        {
            foreach (var (_, media) in content)
                RewriteMediaType(media, inlineable);
        }
    }

    private static void RewriteMediaType(OpenApiMediaType media, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (media.Schema is { } schema)
            media.Schema = RewriteSlot(schema, inlineable);
    }

    private static void RewriteSchema(OpenApiSchema schema, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (schema.Properties is { } properties)
        {
            foreach (var key in properties.Keys.ToList())
                properties[key] = RewriteSlot(properties[key], inlineable);
        }

        if (schema.Items is { } items)
            schema.Items = RewriteSlot(items, inlineable);

        if (schema.AdditionalProperties is { } addProps)
            schema.AdditionalProperties = RewriteSlot(addProps, inlineable);

        RewriteVariantList(schema.AllOf, inlineable);
        RewriteVariantList(schema.OneOf, inlineable);
        RewriteVariantList(schema.AnyOf, inlineable);

        if (schema.Not is { } not)
            schema.Not = RewriteSlot(not, inlineable);
    }

    private static void RewriteVariantList(IList<IOpenApiSchema>? variants, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (variants is null) return;
        for (var i = 0; i < variants.Count; i++)
            variants[i] = RewriteSlot(variants[i], inlineable);
    }

    private static IOpenApiSchema RewriteSlot(IOpenApiSchema slot, IReadOnlyDictionary<string, OpenApiSchema> inlineable)
    {
        if (slot is OpenApiSchemaReference sref && inlineable.TryGetValue(sref.Reference?.Id ?? string.Empty, out var refTarget))
            return CloneWireShape(refTarget);

        if (slot is OpenApiSchema concrete)
        {
            if (TryFindInlineableAllOfRef(concrete, inlineable, out var wrapper))
                return MergeUseSiteWithWrapper(concrete, wrapper);

            RewriteSchema(concrete, inlineable);
            return concrete;
        }

        return slot;
    }

    private static bool TryFindInlineableAllOfRef(OpenApiSchema schema, IReadOnlyDictionary<string, OpenApiSchema> inlineable, out OpenApiSchema wrapper)
    {
        wrapper = null!;
        if (schema.AllOf is not { Count: 1 } allOf) return false;
        if (allOf[0] is not OpenApiSchemaReference sref) return false;
        if (sref.Reference?.Id is not { } id) return false;
        return inlineable.TryGetValue(id, out wrapper!);
    }

    private static OpenApiSchema CloneWireShape(OpenApiSchema source)
    {
        var clone = new OpenApiSchema
        {
            Type = source.Type,
            Format = source.Format,
            MinLength = source.MinLength,
            MaxLength = source.MaxLength,
            MinItems = source.MinItems,
            MaxItems = source.MaxItems,
            Items = source.Items,
            Minimum = source.Minimum,
            Maximum = source.Maximum,
            ExclusiveMinimum = source.ExclusiveMinimum,
            ExclusiveMaximum = source.ExclusiveMaximum,
            Pattern = source.Pattern,
            Description = source.Description,
            Default = source.Default,
        };
        return clone;
    }

    private static OpenApiSchema MergeUseSiteWithWrapper(OpenApiSchema useSite, OpenApiSchema wrapper)
    {
        var result = CloneWireShape(wrapper);

        if (useSite.MinLength is { } minL) SchemaPaint.TightenMinLength(result, minL);
        if (useSite.MaxLength is { } maxL) SchemaPaint.TightenMaxLength(result, maxL);
        if (useSite.MinItems is { } minI) SchemaPaint.TightenMinItems(result, minI);
        if (useSite.MaxItems is { } maxI) SchemaPaint.TightenMaxItems(result, maxI);

        if (TryReadLowerBound(useSite, out var lowerVal, out var lowerExclusive))
            SchemaPaint.TightenLowerBound(result, lowerVal, lowerExclusive);
        if (TryReadUpperBound(useSite, out var upperVal, out var upperExclusive))
            SchemaPaint.TightenUpperBound(result, upperVal, upperExclusive);

        if (!string.IsNullOrEmpty(useSite.Pattern))
            SchemaPaint.SetPatternIfAbsent(result, useSite.Pattern);
        if (!string.IsNullOrEmpty(useSite.Format))
            SchemaPaint.SetFormatIfAbsent(result, useSite.Format);
        if (!string.IsNullOrEmpty(useSite.Description))
            SchemaPaint.SetDescriptionIfAbsent(result, useSite.Description);
        if (useSite.Default is not null)
            SchemaPaint.SetDefaultIfAbsent(result, useSite.Default);

        return result;
    }

    private static bool TryReadLowerBound(OpenApiSchema schema, out decimal value, out bool exclusive)
    {
        var inc = TryParseDecimal(schema.Minimum);
        var exc = TryParseDecimal(schema.ExclusiveMinimum);
        if (exc is not null) { value = exc.Value; exclusive = true; return true; }
        if (inc is not null) { value = inc.Value; exclusive = false; return true; }
        value = 0m; exclusive = false; return false;
    }

    private static bool TryReadUpperBound(OpenApiSchema schema, out decimal value, out bool exclusive)
    {
        var inc = TryParseDecimal(schema.Maximum);
        var exc = TryParseDecimal(schema.ExclusiveMaximum);
        if (exc is not null) { value = exc.Value; exclusive = true; return true; }
        if (inc is not null) { value = inc.Value; exclusive = false; return true; }
        value = 0m; exclusive = false; return false;
    }

    private static decimal? TryParseDecimal(string? value) =>
        value is not null && decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
}
