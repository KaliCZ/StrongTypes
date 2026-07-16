using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Vendor extension flagging a schema as produced by the StrongTypes OpenAPI adapters;
/// the inliner keys on it and strips it, so it never reaches the published document.
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
