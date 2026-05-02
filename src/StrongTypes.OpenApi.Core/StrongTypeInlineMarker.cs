using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Vendor extension that flags a schema as having been produced by the
/// StrongTypes OpenAPI adapters. The inliner uses this marker — not the
/// schema's component name or shape — to decide which schemas it owns and
/// is therefore allowed to inline. The marker is stripped by the inliner,
/// so it never reaches the published document.
/// </summary>
public static class StrongTypeInlineMarker
{
    public const string Key = "x-strongtypes-inline";

    public static void Set(OpenApiSchema schema)
    {
        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions[Key] = new JsonNodeExtension(JsonValue.Create(true));
    }

    public static bool Has(OpenApiSchema schema)
        => schema.Extensions is { } ext && ext.ContainsKey(Key);

    public static void Remove(OpenApiSchema schema) =>
        schema.Extensions?.Remove(Key);
}
