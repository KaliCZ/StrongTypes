using System.Text.Json;
using StrongTypes.OpenApi.IntegrationTests.Helpers;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.BindingSchemaAsserts;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// Asserts the OpenAPI parameter / form-body schemas the pipelines emit for
/// strong-type parameters bound from non-body sources
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>,
/// <c>[FromForm]</c>). Required and nullable variants of each wrapped type
/// are exercised separately so a regression on either branch shows up.
/// </summary>
public abstract partial class OpenApiDocumentTestsBase
{
    // ── [FromQuery] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromQuery_NonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "name");
        AssertNonBodyNonEmptyString(schema);
    }

    [Fact]
    public async Task FromQuery_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableName");
        AssertNonBodyNonEmptyString(schema);
    }

    [Fact]
    public async Task FromQuery_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "count");
        AssertNonBodyPositiveInt(schema, Version);
    }

    [Fact]
    public async Task FromQuery_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableCount");
        AssertNonBodyPositiveInt(schema, Version);
    }

    [Fact]
    public async Task FromQuery_Email_RendersAsString_WithEmailFormat()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "email");
        AssertNonBodyEmail(schema, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromQuery_NullableEmail_RendersAsString_WithEmailFormat()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableEmail");
        AssertNonBodyEmail(schema, IsEmailStringFormatBroken);
    }

    // ── [FromRoute] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromRoute_NonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "name");
        AssertNonBodyNonEmptyString(schema);
    }

    [Fact]
    public async Task FromRoute_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "count");
        AssertRoutePositiveInt(schema, Version);
    }

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public async Task FromHeader_NonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Name");
        AssertNonBodyNonEmptyString(schema);
    }

    [Fact]
    public async Task FromHeader_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Name");
        AssertNonBodyNonEmptyString(schema);
    }

    [Fact]
    public async Task FromHeader_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Count");
        AssertNonBodyPositiveInt(schema, Version);
    }

    [Fact]
    public async Task FromHeader_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Count");
        AssertNonBodyPositiveInt(schema, Version);
    }

    // ── [FromForm] ───────────────────────────────────────────────────────

    // BindingProbeFormRequest declaration order:
    //   0: Name           (NonEmptyString)
    //   1: NullableName   (NonEmptyString?)
    //   2: Count          (Positive<int>)
    //   3: NullableCount  (Positive<int>?)
    //   4: Email          (Email)
    //   5: NullableEmail  (Email?)
    // The form-index is used by the broken-path lookup when the form schema
    // is `allOf:[<each-field>]` with the names dropped — see
    // IsFormPropertiesSchemaBroken.

    [Fact]
    public async Task FromForm_NonEmptyString_RendersAsString_WithMinLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyString(formSchema, "name", allOfIndex: 0, IsFormPropertiesSchemaBroken);
    }

    [Fact]
    public async Task FromForm_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyString(formSchema, "nullableName", allOfIndex: 1, IsFormPropertiesSchemaBroken);
    }

    [Fact]
    public async Task FromForm_PositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveInt(formSchema, "count", allOfIndex: 2, IsFormPropertiesSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveInt(formSchema, "nullableCount", allOfIndex: 3, IsFormPropertiesSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_Email_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmail(formSchema, "email", allOfIndex: 4, IsFormPropertiesSchemaBroken, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromForm_NullableEmail_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmail(formSchema, "nullableEmail", allOfIndex: 5, IsFormPropertiesSchemaBroken, IsEmailStringFormatBroken);
    }

    // ── Caller annotations on non-body slots ─────────────────────────────
    // Strong-type slots bound from query/form should merge caller-supplied
    // data-annotations the same way JSON-body properties do — e.g. a
    // [StringLength(50)] on a [FromQuery] NonEmptyString must reach the
    // wire as maxLength: 50 alongside the wrapper's own minLength: 1.

    [Fact]
    public async Task FromQuery_NonEmptyString_With_StringLength_Carries_Both_Bounds_When_Merged()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "name");
        AssertJsonEquals(schema, IsNonBodyAnnotationMergingBroken
            ? """{"type":"string","minLength":1}"""
            : """{"type":"string","minLength":1,"maxLength":50}""");
    }

    [Fact]
    public async Task FromQuery_PositiveInt_With_Range_Carries_Both_Bounds_When_Merged()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "count");
        AssertJsonEquals(schema, ExpectedAnnotatedRangeShape(IsNonBodyAnnotationMergingBroken, Version));
    }

    [Fact]
    public async Task FromForm_NonEmptyString_With_StringLength_Carries_Both_Bounds_When_Merged()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = ResolveAnnotatedFormProperty(formSchema, "name", allOfIndex: 0);
        AssertJsonEquals(schema, IsNonBodyAnnotationMergingBroken
            ? """{"type":"string","minLength":1}"""
            : """{"type":"string","minLength":1,"maxLength":50}""");
    }

    [Fact]
    public async Task FromForm_PositiveInt_With_Range_Carries_Both_Bounds_When_Merged()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = ResolveAnnotatedFormProperty(formSchema, "count", allOfIndex: 1);
        AssertJsonEquals(schema, ExpectedAnnotatedRangeShape(IsNonBodyAnnotationMergingBroken, Version));
    }

    private static string ExpectedAnnotatedRangeShape(bool mergingBroken, OpenApiVersion version)
    {
        if (!mergingBroken)
            return """{"type":"integer","format":"int32","minimum":5,"maximum":100}""";
        return version switch
        {
            OpenApiVersion.V3_0 => """{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}""",
            OpenApiVersion.V3_1 => """{"type":"integer","format":"int32","exclusiveMinimum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        };
    }

    // The annotated form-probe request has only two fields, so the
    // BindingSchemaAsserts.GetFormProperty helper (which hard-codes the
    // 6-field count for the unannotated probe) doesn't fit; this picks
    // the right slot per pipeline.
    private JsonElement ResolveAnnotatedFormProperty(JsonElement formSchema, string propertyName, int allOfIndex)
    {
        if (IsFormPropertiesSchemaBroken)
            return formSchema.GetProperty("allOf")[allOfIndex];

        var properties = formSchema.GetProperty("properties");
        foreach (var entry in properties.EnumerateObject())
        {
            if (string.Equals(entry.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        Assert.Fail($"form schema has no '{propertyName}' property");
        return default;
    }
}
