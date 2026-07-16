using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Both Microsoft.AspNetCore.OpenApi and Swashbuckle emit form-body property keys in PascalCase (matching the C#
/// property name), so form-property helpers take the name as declared in the source.
/// </summary>
internal static class BindingSchemaAsserts
{
    // ── NonEmptyString ───────────────────────────────────────────────────

    internal static void AssertNonEmptyStringSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1}""");

    internal static void AssertFormPropertyNonEmptyStringSchema(JsonElement formSchema, string propertyName)
        => AssertNonEmptyStringSchema(GetFormProperty(formSchema, propertyName));

    // ── Positive<int> ────────────────────────────────────────────────────

    internal static void AssertPositiveIntSchema(JsonElement schema, OpenApiVersion version)
        => AssertJsonEquals(schema, version switch
        {
            OpenApiVersion.V3_0 => """{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}""",
            OpenApiVersion.V3_1 => """{"type":"integer","format":"int32","exclusiveMinimum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        });

    internal static void AssertFormPropertyPositiveIntSchema(JsonElement formSchema, string propertyName, OpenApiVersion version)
        => AssertPositiveIntSchema(GetFormProperty(formSchema, propertyName), version);

    // ── Digit ────────────────────────────────────────────────────────────

    internal static void AssertDigitSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"integer","format":"int32","minimum":0,"maximum":9}""");

    internal static void AssertFormPropertyDigitSchema(JsonElement formSchema, string propertyName)
        => AssertDigitSchema(GetFormProperty(formSchema, propertyName));

    // ── Other numeric wrappers ──────────────────────────────────────────
    // Inclusive bounds encode identically in 3.0 and 3.1; only exclusive bounds need a version.

    internal static void AssertNonNegativeLongSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"integer","format":"int64","minimum":0}""");

    internal static void AssertNegativeDoubleSchema(JsonElement schema, OpenApiVersion version)
        => AssertJsonEquals(schema, version switch
        {
            OpenApiVersion.V3_0 => """{"type":"number","format":"double","maximum":0,"exclusiveMaximum":true}""",
            OpenApiVersion.V3_1 => """{"type":"number","format":"double","exclusiveMaximum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        });

    // There is no standard OpenAPI format for decimal, so both pipelines fall back to format: double.
    internal static void AssertNonPositiveDecimalSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"number","format":"double","maximum":0}""");

    // ── NonEmptyEnumerable<T> ────────────────────────────────────────────

    internal static void AssertNonEmptyEnumerableOfNonEmptyStringSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"array","minItems":1,"items":{"type":"string","minLength":1}}""");

    internal static void AssertFormPropertyNonEmptyEnumerableOfNonEmptyStringSchema(JsonElement formSchema, string propertyName)
        => AssertNonEmptyEnumerableOfNonEmptyStringSchema(GetFormProperty(formSchema, propertyName));

    internal static void AssertPlainDecimalSchema(JsonElement schema, OpenApiVersion version)
    {
        if (version == OpenApiVersion.V3_1 && schema.TryGetProperty("pattern", out _) && schema.TryGetProperty("type", out _))
        {
            AssertJsonEquals(schema, """{"pattern":"^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$","type":["number","string"],"format":"double"}""");
            return;
        }

        if (schema.TryGetProperty("pattern", out _))
        {
            AssertJsonEquals(schema, """{"pattern":"^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$","format":"double"}""");
            return;
        }

        AssertJsonEquals(schema, """{"type":"number","format":"double"}""");
    }

    // ── Email ────────────────────────────────────────────────────────────

    internal static void AssertEmailSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1,"maxLength":254,"format":"email"}""");

    internal static void AssertFormPropertyEmailSchema(JsonElement formSchema, string propertyName)
        => AssertEmailSchema(GetFormProperty(formSchema, propertyName));

    private static JsonElement GetFormProperty(JsonElement formSchema, string propertyName)
        => formSchema.GetProperty("properties").GetProperty(propertyName);

    /// <summary>Asserts the named property in the schema's <c>properties</c> map deep-equals the literal JSON snapshot.</summary>
    internal static void AssertSchema(JsonElement formSchema, string propertyName, string expectedJson)
        => AssertJsonEquals(GetFormProperty(formSchema, propertyName), expectedJson);

    internal static void AssertFormBodyHasObjectShape(JsonElement formSchema, params string[] expectedPropertyNames)
    {
        Assert.False(formSchema.TryGetProperty("allOf", out _), "form body should not be wrapped in a top-level allOf");
        Assert.False(formSchema.TryGetProperty("anyOf", out _), "form body should not be wrapped in a top-level anyOf");
        Assert.False(formSchema.TryGetProperty("oneOf", out _), "form body should not be wrapped in a top-level oneOf");
        Assert.False(formSchema.TryGetProperty("$ref", out _), "form body should be inlined, not a $ref");
        Assert.True(formSchema.TryGetProperty("properties", out var properties), "form body must have a properties map");
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);

        var actual = properties.EnumerateObject().Select(p => p.Name);
        Assert.Equivalent(expectedPropertyNames, actual);
    }
}
