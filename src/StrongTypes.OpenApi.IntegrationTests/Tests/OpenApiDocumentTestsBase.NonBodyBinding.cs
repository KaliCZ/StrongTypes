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
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromQuery_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableName");
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromQuery_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "count");
        AssertPositiveIntSchema(schema, Version);
    }

    [Fact]
    public async Task FromQuery_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableCount");
        AssertPositiveIntSchema(schema, Version);
    }

    [Fact]
    public async Task FromQuery_Email_RendersAsString_WithEmailFormat()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "email");
        AssertEmailSchema(schema, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromQuery_NullableEmail_RendersAsString_WithEmailFormat()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableEmail");
        AssertEmailSchema(schema, IsEmailStringFormatBroken);
    }

    // ── [FromRoute] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromRoute_NonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "name");
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromRoute_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "count");
        AssertPositiveIntSchema(schema, Version);
    }

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public async Task FromHeader_NonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Name");
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromHeader_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Name");
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromHeader_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Count");
        AssertPositiveIntSchema(schema, Version);
    }

    [Fact]
    public async Task FromHeader_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Count");
        AssertPositiveIntSchema(schema, Version);
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
        AssertFormPropertyNonEmptyStringSchema(formSchema, "name", allOfIndex: 0, IsFormPropertiesSchemaBroken);
    }

    [Fact]
    public async Task FromForm_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyStringSchema(formSchema, "nullableName", allOfIndex: 1, IsFormPropertiesSchemaBroken);
    }

    [Fact]
    public async Task FromForm_PositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveIntSchema(formSchema, "count", allOfIndex: 2, IsFormPropertiesSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveIntSchema(formSchema, "nullableCount", allOfIndex: 3, IsFormPropertiesSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_Email_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmailSchema(formSchema, "email", allOfIndex: 4, IsFormPropertiesSchemaBroken, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromForm_NullableEmail_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmailSchema(formSchema, "nullableEmail", allOfIndex: 5, IsFormPropertiesSchemaBroken, IsEmailStringFormatBroken);
    }

    // ── Caller annotations on non-body slots ─────────────────────────────
    // Strong-type slots bound from query/form should merge caller-supplied
    // data-annotations the same way JSON-body properties do — e.g. a
    // [StringLength(50)] on a [FromQuery] NonEmptyString must reach the
    // wire as maxLength: 50 alongside the wrapper's own minLength: 1.
    //
    // Each wrapper-typed test has a primitive-typed sibling that pins the
    // baseline: the framework natively surfaces the annotation on the
    // primitive, so the wrapper test only has teeth when the primitive
    // baseline carries the keyword. If a pipeline ever stops surfacing the
    // annotation on the primitive, the baseline fails first and the
    // wrapper failure is correctly attributed to the framework, not us.

    [Fact]
    public async Task FromQuery_PlainString_With_StringLength_Carries_MaxLength()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "plainName");
        Assert.Equal(50, schema.GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public async Task FromQuery_NonEmptyString_With_StringLength_Carries_Both_Bounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "name");
        AssertJsonEquals(schema, """{"type":"string","minLength":1,"maxLength":50}""");
    }

    [Fact]
    public async Task FromQuery_PlainInt_With_Range_Carries_Both_Bounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "plainCount");
        Assert.Equal(5, schema.GetProperty("minimum").GetInt32());
        Assert.Equal(100, schema.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public async Task FromQuery_PositiveInt_With_Range_Carries_Both_Bounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query-annotated", "count");
        AssertJsonEquals(schema, """{"type":"integer","format":"int32","minimum":5,"maximum":100}""");
    }

    [Fact]
    public async Task FromForm_PlainString_With_StringLength_Carries_MaxLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated-plain");
        var schema = formSchema.GetProperty("properties").GetProperty(FormPropertyName("PlainName"));
        Assert.Equal(50, schema.GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public async Task FromForm_NonEmptyString_With_StringLength_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = ResolveAnnotatedFormWrapperProperty(formSchema, "Name", allOfIndex: 0);
        AssertJsonEquals(schema, """{"type":"string","minLength":1,"maxLength":50}""");
    }

    [Fact]
    public async Task FromForm_PlainInt_With_Range_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated-plain");
        var schema = formSchema.GetProperty("properties").GetProperty(FormPropertyName("PlainCount"));
        Assert.Equal(5, schema.GetProperty("minimum").GetInt32());
        Assert.Equal(100, schema.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public async Task FromForm_PositiveInt_With_Range_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = ResolveAnnotatedFormWrapperProperty(formSchema, "Count", allOfIndex: 1);
        AssertJsonEquals(schema, """{"type":"integer","format":"int32","minimum":5,"maximum":100}""");
    }

    private JsonElement ResolveAnnotatedFormWrapperProperty(JsonElement formSchema, string propertyName, int allOfIndex)
    {
        // For the wrappers-only annotated form, Swashbuckle emits the
        // broken `{allOf:[<each>]}` shape (every field is component-typed),
        // while Microsoft emits a proper properties map.
        if (IsFormPropertiesSchemaBroken)
            return formSchema.GetProperty("allOf")[allOfIndex];

        return formSchema.GetProperty("properties").GetProperty(FormPropertyName(propertyName));
    }

    protected virtual string FormPropertyName(string clrPropertyName) => clrPropertyName;
}
