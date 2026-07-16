using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

// The framework's schema-deduplication pass copies wrapper schemas into components.schemas
// without our schema-transformer mutations, leaving `{}`; repaint each from its component name.
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
        // Bail when the name is already taken by a user DTO (e.g. a class named "Email") so we don't trample its schema.
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

        if (name == "Digit")
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Minimum = "0",
                Maximum = "9",
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
        if (props.Count == 1 && (props.ContainsKey("Value") || props.ContainsKey("value"))) return false;
        return true;
    }

    private static IOpenApiSchema ResolveInner(string innerName, IDictionary<string, IOpenApiSchema> components)
    {
        if (MicrosoftSchemaNaming.TryGetPrimitiveInfo(innerName, out var prim))
            return new OpenApiSchema { Type = prim.Type, Format = prim.Format };

        var built = TryBuild(innerName, components);
        if (built is not null) return built;

        // Unfamiliar names fall back to a $ref; the framework's own component supplies the body.
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
