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
/// The form helpers additionally navigate the request-body schema:
/// pipelines may emit <c>{ properties: { … } }</c> (Microsoft, modern
/// Swashbuckle) or <c>{ allOf: [ &lt;each-field&gt; ] }</c> with the
/// field names dropped (vanilla Swashbuckle when every field is
/// component-typed — see <see cref="OpenApiDocumentTestsBase.IsFormPropertiesSchemaBroken"/>).
/// The helpers pick the right slot per pipeline and then run the same
/// shape assertion as the parameter-side helpers.
/// </summary>
internal static class BindingSchemaAsserts
{
    /// <summary>
    /// Number of fields in <c>BindingProbeFormRequest</c>. The Swashbuckle
    /// broken-path navigates the form schema's <c>allOf</c> by declaration
    /// index and asserts this count to lock in that the broken shape is the
    /// one we expect.
    /// </summary>
    private const int FormFieldCount = 6;

    // ── NonEmptyString ───────────────────────────────────────────────────

    internal static void AssertNonEmptyStringSchema(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1}""");

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyNonEmptyStringSchema(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken)
        => AssertNonEmptyStringSchema(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken));

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

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyPositiveIntSchema(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, OpenApiVersion version)
        => AssertPositiveIntSchema(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), version);

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

    // ── Email ────────────────────────────────────────────────────────────

    internal static void AssertEmailSchema(JsonElement schema, bool isEmailStringFormatBroken)
        => AssertJsonEquals(schema, isEmailStringFormatBroken
            ? """{"type":"string","minLength":1,"maxLength":254}"""
            : """{"type":"string","minLength":1,"maxLength":254,"format":"email"}""");

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyEmailSchema(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isEmailStringFormatBroken)
        => AssertEmailSchema(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), isEmailStringFormatBroken);

    /// <summary>
    /// Looks up a per-field schema on a <c>[FromForm]</c> request-body
    /// schema. On the not-broken path the form schema is an object with a
    /// <c>properties</c> map; the field is found by name (case-insensitive
    /// because Microsoft emits PascalCase, Swashbuckle camelCase). On the
    /// broken path the form schema is <c>{ allOf: [&lt;each-field&gt;] }</c>
    /// with names dropped; the field is found by its declaration index
    /// (<paramref name="allOfIndex"/>).
    /// </summary>
    private static JsonElement GetFormProperty(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken)
    {
        if (isFormPropertiesSchemaBroken)
        {
            Assert.True(formSchema.TryGetProperty("allOf", out var allOf), "form schema is broken-flagged but has no allOf");
            Assert.Equal(JsonValueKind.Array, allOf.ValueKind);
            Assert.Equal(FormFieldCount, allOf.GetArrayLength());
            Assert.False(formSchema.TryGetProperty("properties", out _), "form schema is broken-flagged but still has a properties map");
            return allOf[allOfIndex];
        }

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
