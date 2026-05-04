using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Schema-shape assertions for parameters bound from non-body sources
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>) and for
/// individual properties of <c>[FromForm]</c> request bodies.
///
/// Every shape helper deep-compares the schema against a literal JSON
/// snapshot via <see cref="AssertJsonEquals"/>, so the helper body reads
/// as the exact expected schema and any unexpected keyword fails the test.
///
/// Per-source splits exist where the broken shape genuinely differs by
/// binding source — most notably <see cref="AssertRoutePositiveInt"/>,
/// where a route <c>:int</c> constraint preserves the integer encoding
/// even on pipelines that don't (yet) honour the strong-type transformer
/// for non-body slots. The flag-driven branches that lived here for the
/// pre-#87 Microsoft pipeline have been removed: every supported pipeline
/// runs a non-body strong-type painter, and any future regression on that
/// painter shows up as a literal-schema mismatch on these helpers.
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

    internal static void AssertNonBodyNonEmptyString(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1}""");

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyNonEmptyString(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken)
        => AssertNonBodyNonEmptyString(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken));

    // ── Positive<int> ────────────────────────────────────────────────────
    // Splits by OpenAPI version: 3.0 encodes the exclusive bound as
    // {minimum:0, exclusiveMinimum:true} (boolean pair); 3.1 as
    // {exclusiveMinimum:0} (numeric).

    /// <summary>
    /// Asserts the schema for a <c>[FromQuery]</c> / <c>[FromHeader]</c> /
    /// <c>[FromRoute]</c> <c>Positive&lt;int&gt;</c> parameter.
    /// </summary>
    internal static void AssertNonBodyPositiveInt(JsonElement schema, OpenApiVersion version)
        => AssertJsonEquals(schema, version switch
        {
            OpenApiVersion.V3_0 => """{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}""",
            OpenApiVersion.V3_1 => """{"type":"integer","format":"int32","exclusiveMinimum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        });

    /// <summary>
    /// Same wire shape as <see cref="AssertNonBodyPositiveInt"/>; kept as
    /// a separate entry point because the route helper used to assert a
    /// pipeline-specific broken shape and call sites still document the
    /// binding source they're exercising.
    /// </summary>
    internal static void AssertRoutePositiveInt(JsonElement schema, OpenApiVersion version)
        => AssertNonBodyPositiveInt(schema, version);

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyPositiveInt(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, OpenApiVersion version)
        => AssertNonBodyPositiveInt(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), version);

    // ── Email ────────────────────────────────────────────────────────────

    internal static void AssertNonBodyEmail(JsonElement schema, bool isEmailStringFormatBroken)
        => AssertJsonEquals(schema, isEmailStringFormatBroken
            ? """{"type":"string","minLength":1,"maxLength":254}"""
            : """{"type":"string","minLength":1,"maxLength":254,"format":"email"}""");

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyEmail(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isEmailStringFormatBroken)
        => AssertNonBodyEmail(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), isEmailStringFormatBroken);

    /// <summary>
    /// Looks up a per-field schema on a <c>[FromForm]</c> request-body
    /// schema. On the not-broken path the form schema is an object with a
    /// <c>properties</c> map; the field is found by name (case-insensitive
    /// because Microsoft emits PascalCase, Swashbuckle camelCase). On the
    /// broken path the form schema is <c>{ allOf: [&lt;each-field&gt;] }</c>
    /// with names dropped; the field is found by its declaration index
    /// (<paramref name="allOfIndex"/>). Either way, the per-field schema
    /// returned is then asserted with the same strong-type assertion.
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
