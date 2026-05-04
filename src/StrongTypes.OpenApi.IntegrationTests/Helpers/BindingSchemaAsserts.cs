using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Centralised wire-shape assertions for the strong-type wrappers, used
/// by body, parameter, and form-property tests alike — the wrapper's
/// wire shape doesn't depend on where it's bound. Every helper
/// deep-compares against a literal JSON snapshot via
/// <see cref="AssertJsonEquals"/> so an unexpected keyword fails the test
/// instead of silently passing.
///
/// The form helpers additionally navigate the request-body schema by
/// looking up the field by name in the form's <c>properties</c> map.
/// Casing is treated case-insensitively because Microsoft emits
/// PascalCase and Swashbuckle camelCase.
/// </summary>
internal static class BindingSchemaAsserts
{
    // ── NonEmptyString ───────────────────────────────────────────────────

    internal static void AssertNonEmptyStringSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1}""");

    internal static void AssertFormPropertyNonEmptyStringSchema(JsonElement formSchema, string propertyName)
        => AssertNonEmptyStringSchema(GetFormProperty(formSchema, propertyName));

    // ── Positive<int> ────────────────────────────────────────────────────
    // Splits by OpenAPI version: 3.0 encodes the exclusive bound as
    // {minimum:0, exclusiveMinimum:true} (boolean pair); 3.1 as
    // {exclusiveMinimum:0} (numeric).

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
    // Inclusive-bound shapes (NonNegative, NonPositive) don't depend on
    // OpenAPI version — there's no exclusive boundary to encode. Exclusive
    // shapes (Negative<double>) split the same way Positive does.

    internal static void AssertNonNegativeLongSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"integer","format":"int64","minimum":0}""");

    internal static void AssertNegativeDoubleSchema(JsonElement schema, OpenApiVersion version)
        => AssertJsonEquals(schema, version switch
        {
            OpenApiVersion.V3_0 => """{"type":"number","format":"double","maximum":0,"exclusiveMaximum":true}""",
            OpenApiVersion.V3_1 => """{"type":"number","format":"double","exclusiveMaximum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        });

    // Both pipelines emit `format: double` for `decimal`. There's no
    // standard OpenAPI format for the BCL decimal type, so they fall
    // through to the closest numeric format. Pinned here to surface a
    // future change in pipeline behaviour rather than to bless the
    // mapping as ideal.
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

    internal static void AssertEmailSchema(JsonElement schema, bool isEmailStringFormatBroken)
        => AssertJsonEquals(schema, isEmailStringFormatBroken
            ? """{"type":"string","minLength":1,"maxLength":254}"""
            : """{"type":"string","minLength":1,"maxLength":254,"format":"email"}""");

    internal static void AssertFormPropertyEmailSchema(JsonElement formSchema, string propertyName, bool isEmailStringFormatBroken)
        => AssertEmailSchema(GetFormProperty(formSchema, propertyName), isEmailStringFormatBroken);

    /// <summary>
    /// Looks up a per-field schema on a <c>[FromForm]</c> request-body
    /// schema. The field is found by name (case-insensitive because
    /// Microsoft emits PascalCase, Swashbuckle camelCase).
    /// </summary>
    private static JsonElement GetFormProperty(JsonElement formSchema, string propertyName)
    {
        var properties = formSchema.GetProperty("properties");
        foreach (var entry in properties.EnumerateObject())
        {
            if (string.Equals(entry.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        Assert.Fail($"form schema has no '{propertyName}' property (case-insensitive)");
        return default;
    }
}
