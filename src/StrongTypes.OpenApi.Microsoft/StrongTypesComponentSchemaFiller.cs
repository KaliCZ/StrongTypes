using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

// The framework's schema-deduplication pass copies repeated wrapper schemas
// (Maybe<T>, Positive<T>, …) into components.schemas with a fresh
// OpenApiSchema instance — that copy doesn't pick up our schema-transformer
// mutations on certain wrapper types, leaving the component as `{}`. This
// document transformer runs after deduplication and reverses each
// strong-type-derived component name back into the wire schema the converter
// actually emits, so referenced schemas match the inline ones.
internal sealed class StrongTypesComponentSchemaFiller : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is not { } schemas) return Task.CompletedTask;

        foreach (var (name, sch) in schemas)
        {
            if (sch is not OpenApiSchema concrete) continue;
            TryPaint(name, concrete, schemas);
        }

        return Task.CompletedTask;
    }

    private static bool TryPaint(string name, OpenApiSchema schema, IDictionary<string, IOpenApiSchema> components)
    {
        var painted = TryBuild(name, components);
        if (painted is null) return false;

        SchemaPaint.ClearWrapperShape(schema);
        CopyInto(schema, painted);
        StrongTypeInlineMarker.Set(schema);
        return true;
    }

    private static OpenApiSchema? TryBuild(string name, IDictionary<string, IOpenApiSchema> components)
    {
        // Name-only matching would happily trample a user DTO that happens
        // to be called `Email` or `MaybeOfInt32`. Every wrapper we paint is
        // a primitive (string / integer / number / array) or — for
        // `MaybeOf<T>` — an object with a single `Value` property. An object
        // schema with multiple/non-`Value` properties under our reserved
        // name is a user DTO, not ours; bail. Returning null also makes
        // ResolveInner fall through to a `$ref` so nested references to a
        // colliding user DTO point at their schema instead of ours.
        if (components.TryGetValue(name, out var existing)
            && existing is OpenApiSchema concrete
            && LooksLikeUserDto(concrete))
            return null;

        if (name == "NonEmptyString")
            return new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 };

        if (name == "Email")
            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "email",
                MinLength = 1,
                MaxLength = Email.MaxLength,
            };

        if (MicrosoftSchemaNaming.TryMatchNumericComponent(name, out var numericInner, out var numericBound))
            return BuildNumeric(numericInner, numericBound);

        if (TrySplit(name, "NonEmptyEnumerableOf", out var arrayInner))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                MinItems = 1,
                Items = ResolveInner(arrayInner, components),
            };
        }

        if (TrySplit(name, "MaybeOf", out var maybeInner))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["Value"] = ResolveInner(maybeInner, components),
                },
            };
        }

        return null;
    }

    private static bool LooksLikeUserDto(OpenApiSchema schema)
    {
        if (schema.Type != JsonSchemaType.Object) return false;
        if (schema.Properties is not { Count: > 0 } props) return false;
        if (props.Count == 1 && props.ContainsKey("Value")) return false;
        return true;
    }

    private static IOpenApiSchema ResolveInner(string innerName, IDictionary<string, IOpenApiSchema> components)
    {
        if (MicrosoftSchemaNaming.TryGetPrimitiveInfo(innerName, out var prim))
            return new OpenApiSchema { Type = prim.Type, Format = prim.Format };

        var built = TryBuild(innerName, components);
        if (built is not null) return built;

        // Fall back to a $ref so unfamiliar type names still produce a valid
        // schema (and the framework's own component will supply the body).
        return new OpenApiSchemaReference(innerName);
    }

    private static OpenApiSchema BuildNumeric(string innerKeyword, NumericBound bound)
    {
        var info = MicrosoftSchemaNaming.TryGetPrimitiveInfo(innerKeyword, out var p) ? p : new PrimitiveSchemaMap.Info(JsonSchemaType.Number, null);
        var schema = new OpenApiSchema();
        NumericWrapperPainter.Paint(schema, info, bound);
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
