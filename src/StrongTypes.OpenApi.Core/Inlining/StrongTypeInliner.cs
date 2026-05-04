using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Walks an <see cref="OpenApiDocument"/> and replaces every <c>$ref</c> to
/// a strong-type wrapper component with the wrapper's wire body, merging any
/// caller-supplied annotations attached at the use site (the <c>allOf:[ref]</c>
/// + <c>maxLength</c>/<c>maxItems</c>/etc. shape that the property-annotation
/// transformers emit). Both wrapper components and use-site allOf wrappers
/// are identified by the <see cref="StrongTypeInlineMarker"/> vendor extension
/// — the inliner does not match on schema names. After all references are
/// inlined, the wrapper components themselves are dropped from
/// <c>components.schemas</c>.
/// </summary>
public static class StrongTypeInliner
{
    public static void Inline(OpenApiDocument document, ILogger? logger = null)
    {
        if (document.Components?.Schemas is not { } schemas) return;

        var inlineable = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        foreach (var (name, schema) in schemas)
        {
            if (schema is OpenApiSchema concrete && StrongTypeInlineMarker.Has(concrete))
                inlineable[name] = concrete;
        }
        if (inlineable.Count == 0) return;

        var ctx = new RewriteContext(inlineable, logger);

        // Walk every component (including the inlineable bodies themselves)
        // so refs to other inlineable wrappers nested inside an array
        // wrapper's `items` get resolved before we use the wrapper as a
        // template at use sites.
        foreach (var (_, schema) in schemas)
        {
            if (schema is OpenApiSchema concrete) RewriteSchema(concrete, ctx);
        }

        if (document.Paths is not null)
        {
            foreach (var (_, pathItem) in document.Paths)
                RewritePathItem(pathItem, ctx);
        }

        foreach (var name in inlineable.Keys)
            schemas.Remove(name);
    }

    private readonly record struct RewriteContext(IReadOnlyDictionary<string, OpenApiSchema> Inlineable, ILogger? Logger);

    private static void RewritePathItem(IOpenApiPathItem pathItem, RewriteContext ctx)
    {
        if (pathItem.Operations is { } operations)
        {
            foreach (var (_, operation) in operations)
                RewriteOperation(operation, ctx);
        }

        if (pathItem.Parameters is { } pathParams)
        {
            foreach (var p in pathParams)
                RewriteParameter(p, ctx);
        }
    }

    private static void RewriteOperation(OpenApiOperation operation, RewriteContext ctx)
    {
        if (operation.Parameters is { } parameters)
        {
            foreach (var p in parameters)
                RewriteParameter(p, ctx);
        }

        if (operation.RequestBody is { Content: { } reqContent })
        {
            foreach (var (_, media) in reqContent)
                RewriteMediaType(media, ctx);
        }

        if (operation.Responses is { } responses)
        {
            foreach (var (_, response) in responses)
            {
                if (response.Content is { } respContent)
                {
                    foreach (var (_, media) in respContent)
                        RewriteMediaType(media, ctx);
                }
                if (response.Headers is { } headers)
                {
                    foreach (var (_, header) in headers)
                    {
                        if (header is OpenApiHeader concreteHeader && concreteHeader.Schema is { } headerSchema)
                            concreteHeader.Schema = RewriteSlot(headerSchema, ctx);
                    }
                }
            }
        }
    }

    private static void RewriteParameter(IOpenApiParameter parameter, RewriteContext ctx)
    {
        if (parameter is OpenApiParameter concrete && concrete.Schema is { } schema)
            concrete.Schema = RewriteSlot(schema, ctx);

        if (parameter.Content is { } content)
        {
            foreach (var (_, media) in content)
                RewriteMediaType(media, ctx);
        }
    }

    private static void RewriteMediaType(OpenApiMediaType media, RewriteContext ctx)
    {
        if (media.Schema is { } schema)
            media.Schema = RewriteSlot(schema, ctx);
    }

    private static void RewriteSchema(OpenApiSchema schema, RewriteContext ctx)
    {
        if (schema.Properties is { } properties)
        {
            foreach (var key in properties.Keys.ToList())
                properties[key] = RewriteSlot(properties[key], ctx);
        }

        if (schema.Items is { } items)
            schema.Items = RewriteSlot(items, ctx);

        if (schema.AdditionalProperties is { } addProps)
            schema.AdditionalProperties = RewriteSlot(addProps, ctx);

        RewriteVariantList(schema.AllOf, ctx);
        RewriteVariantList(schema.OneOf, ctx);
        RewriteVariantList(schema.AnyOf, ctx);

        if (schema.Not is { } not)
            schema.Not = RewriteSlot(not, ctx);

        StrongTypeInlineMarker.Remove(schema);
    }

    private static void RewriteVariantList(IList<IOpenApiSchema>? variants, RewriteContext ctx)
    {
        if (variants is null) return;
        for (var i = 0; i < variants.Count; i++)
            variants[i] = RewriteSlot(variants[i], ctx);
    }

    private static IOpenApiSchema RewriteSlot(IOpenApiSchema slot, RewriteContext ctx)
    {
        if (slot is OpenApiSchemaReference sref && ctx.Inlineable.TryGetValue(sref.Reference?.Id ?? string.Empty, out var refTarget))
            return CloneWireShape(refTarget);

        if (slot is OpenApiSchema concrete)
        {
            if (TryFindInlineableAllOfRef(concrete, ctx, out var wrapper))
                return MergeUseSiteWithWrapper(concrete, wrapper);

            RewriteSchema(concrete, ctx);
            return concrete;
        }

        return slot;
    }

    private static bool TryFindInlineableAllOfRef(OpenApiSchema schema, RewriteContext ctx, out OpenApiSchema wrapper)
    {
        wrapper = null!;
        if (!StrongTypeInlineMarker.Has(schema)) return false;

        if (schema.AllOf is not { Count: 1 } allOf
            || allOf[0] is not OpenApiSchemaReference sref
            || sref.Reference?.Id is not { } id)
        {
            ctx.Logger?.LogError(
                "StrongTypes-marked use-site wrapper has unexpected shape (AllOf.Count={Count}, FirstIsRef={IsRef}). " +
                "Another OpenAPI transformer or filter likely mutated a schema produced by Kalicz.StrongTypes after our adapter ran. " +
                "Caller annotations on this slot will not be merged with the wrapper's wire form.",
                schema.AllOf?.Count ?? 0,
                schema.AllOf is { Count: > 0 } && schema.AllOf[0] is OpenApiSchemaReference);
            return false;
        }

        return ctx.Inlineable.TryGetValue(id, out wrapper!);
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
            Properties = source.Properties is null
                ? null
                : new Dictionary<string, IOpenApiSchema>(source.Properties, StringComparer.Ordinal),
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
