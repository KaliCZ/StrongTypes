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
    public async Task FromQuery_Digit_RendersAsInteger_WithZeroToNineBounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "digit");
        AssertDigitSchema(schema);
    }

    [Fact]
    public async Task FromQuery_NullableDigit_RendersAsInteger_WithZeroToNineBounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableDigit");
        AssertDigitSchema(schema);
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
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}/{digit}", "name");
        AssertNonEmptyStringSchema(schema);
    }

    [Fact]
    public async Task FromRoute_PositiveInt_RendersAsExclusivePositive()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}/{digit}", "count");
        AssertPositiveIntSchema(schema, Version);
    }

    [Fact]
    public async Task FromRoute_Digit_RendersAsInteger_WithZeroToNineBounds()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}/{digit}", "digit");
        AssertDigitSchema(schema);
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

    [Fact]
    public async Task FromForm_NonEmptyString_RendersAsString_WithMinLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyStringSchema(formSchema, "name");
    }

    [Fact]
    public async Task FromForm_NullableNonEmptyString_RendersAsString_WithMinLength()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyStringSchema(formSchema, "nullableName");
    }

    [Fact]
    public async Task FromForm_PositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveIntSchema(formSchema, "count", Version);
    }

    [Fact]
    public async Task FromForm_NullablePositiveInt_RendersAsExclusivePositive()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveIntSchema(formSchema, "nullableCount", Version);
    }

    [Fact]
    public async Task FromForm_Digit_RendersAsInteger_WithZeroToNineBounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyDigitSchema(formSchema, "digit");
    }

    [Fact]
    public async Task FromForm_NonEmptyEnumerable_RendersAsArray_WithMinItems_And_StringItems()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyEnumerableOfNonEmptyStringSchema(formSchema, "tags");
    }

    [Fact]
    public async Task FromForm_Email_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmailSchema(formSchema, "email", IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromForm_NullableEmail_RendersAsString_WithEmailFormat()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmailSchema(formSchema, "nullableEmail", IsEmailStringFormatBroken);
    }

    /// <summary>
    /// The all-wrapper form body schema is a regular
    /// <c>{ type: object, properties: { … } }</c> shape with one entry per
    /// field. Swashbuckle's stock form-body assembler emits a nameless
    /// <c>{ allOf: [&lt;each&gt;] }</c> instead whenever every field is
    /// component-typed; <c>NonBodyStrongTypeOperationFilter</c> rebuilds
    /// the properties map. This pins the aggregate shape so a regression
    /// on either pipeline shows up as a missing property rather than as
    /// per-field assertions silently looking up the wrong slot.
    /// </summary>
    [Fact]
    public async Task FromForm_AllWrappers_FormBody_IsObjectWithPropertiesMap()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormBodyHasObjectShape(formSchema,
            "Name", "NullableName", "Count", "NullableCount", "Digit", "Tags", "Email", "NullableEmail");
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
        AssertJsonEquals(schema, """{"type":"string","minLength":0,"maxLength":50}""");
    }

    [Fact]
    public async Task FromForm_NonEmptyString_With_StringLength_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = formSchema.GetProperty("properties").GetProperty(FormPropertyName("Name"));
        AssertJsonEquals(schema, """{"type":"string","minLength":1,"maxLength":50}""");
    }

    [Fact]
    public async Task FromForm_PlainInt_With_Range_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated-plain");
        var schema = formSchema.GetProperty("properties").GetProperty(FormPropertyName("PlainCount"));
        AssertJsonEquals(schema, IsPlainIntFormSchemaMissingType
            ? Version == OpenApiVersion.V3_1
                ? """{"pattern":"^-?(?:0|[1-9]\\d*)$","type":["integer","string"],"format":"int32","minimum":5,"maximum":100}"""
                : """{"pattern":"^-?(?:0|[1-9]\\d*)$","format":"int32","minimum":5,"maximum":100}"""
            : """{"type":"integer","format":"int32","minimum":5,"maximum":100}""");
    }

    [Fact]
    public async Task FromForm_PositiveInt_With_Range_Carries_Both_Bounds()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-annotated");
        var schema = formSchema.GetProperty("properties").GetProperty(FormPropertyName("Count"));
        AssertJsonEquals(schema, """{"type":"integer","format":"int32","minimum":5,"maximum":100}""");
    }

    // ── Diverse form-body shapes ─────────────────────────────────────────
    // The aggregate-shape test above pins the all-wrapper form body. The
    // three tests below exercise the form-body reshape on more diverse
    // payloads: a primitives-only form (Swashbuckle emits a clean
    // properties map natively), an all-wrappers form where multiple
    // NonEmptyStrings each carry a different annotation
    // (RegularExpression, Url, StringLength) alongside Email,
    // Positive<int>, and a NonEmptyEnumerable of a numeric wrapper, and a
    // mixed form combining both kinds with caller annotations on each.
    // Each test pins every property's full schema so a regression on
    // either pipeline shows up as a per-property diff.

    [Fact]
    public async Task FromForm_SimpleTypes_FormBody_RendersAllPropertiesWithTheirAnnotations()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-simple-types");
        AssertFormBodyHasObjectShape(formSchema, "Title", "Age", "IsActive", "Description");

        var properties = formSchema.GetProperty("properties");

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Title")), """{"type":"string","minLength":0,"maxLength":50}""");
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Description")), """{"type":"string","minLength":0,"maxLength":200}""");

        var isActive = properties.GetProperty(FormPropertyName("IsActive"));
        Assert.Equal("boolean", isActive.GetProperty("type").GetString());

        var age = properties.GetProperty(FormPropertyName("Age"));
        AssertJsonEquals(age, IsPlainIntFormSchemaMissingType
            ? Version == OpenApiVersion.V3_1
                ? """{"pattern":"^-?(?:0|[1-9]\\d*)$","type":["integer","string"],"format":"int32","minimum":0,"maximum":150}"""
                : """{"pattern":"^-?(?:0|[1-9]\\d*)$","format":"int32","minimum":0,"maximum":150}"""
            : """{"type":"integer","format":"int32","minimum":0,"maximum":150}""");
    }

    [Fact]
    public async Task FromForm_AllStrongTypes_FormBody_RendersEveryWrapperWithItsAnnotation()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-all-wrappers");
        AssertFormBodyHasObjectShape(formSchema, "Code", "Website", "Description", "Quantity", "Contact", "Losses");

        var properties = formSchema.GetProperty("properties");

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Code")), """{"type":"string","minLength":1,"pattern":"^[A-Z]{3}-\\d{4}$"}""");
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Website")), """{"type":"string","minLength":1,"format":"uri"}""");
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Description")), """{"type":"string","minLength":1,"maxLength":200}""");
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Quantity")), """{"type":"integer","format":"int32","minimum":1,"maximum":100}""");

        AssertEmailSchema(properties.GetProperty(FormPropertyName("Contact")), IsEmailStringFormatBroken);

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Losses")), Version switch
        {
            OpenApiVersion.V3_0 => """{"type":"array","minItems":2,"items":{"type":"number","format":"double","maximum":0,"exclusiveMaximum":true}}""",
            OpenApiVersion.V3_1 => """{"type":"array","minItems":2,"items":{"type":"number","format":"double","exclusiveMaximum":0}}""",
            _ => throw new ArgumentOutOfRangeException(nameof(Version), Version, null),
        });
    }

    [Fact]
    public async Task FromForm_Mixed_FormBody_RendersBothPrimitivesAndWrappersWithAnnotations()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form-mixed");
        AssertFormBodyHasObjectShape(formSchema, "Title", "Code", "Quantity", "Stock", "ContactEmail", "Tags");

        var properties = formSchema.GetProperty("properties");

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Title")), """{"type":"string","minLength":0,"maxLength":100}""");
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Code")), """{"type":"string","minLength":1,"pattern":"^[A-Z]{3}$"}""");

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Quantity")), IsPlainIntFormSchemaMissingType
            ? Version == OpenApiVersion.V3_1
                ? """{"pattern":"^-?(?:0|[1-9]\\d*)$","type":["integer","string"],"format":"int32","minimum":1,"maximum":1000}"""
                : """{"pattern":"^-?(?:0|[1-9]\\d*)$","format":"int32","minimum":1,"maximum":1000}"""
            : """{"type":"integer","format":"int32","minimum":1,"maximum":1000}""");

        AssertJsonEquals(properties.GetProperty(FormPropertyName("Stock")), """{"type":"integer","format":"int32","minimum":1,"maximum":100}""");
        AssertEmailSchema(properties.GetProperty(FormPropertyName("ContactEmail")), IsEmailStringFormatBroken);
        AssertJsonEquals(properties.GetProperty(FormPropertyName("Tags")), """{"type":"array","minItems":1,"items":{"type":"string","minLength":1}}""");
    }

    private static void AssertFormBodyHasObjectShape(JsonElement formSchema, params string[] expectedPropertyNames)
    {
        Assert.False(formSchema.TryGetProperty("allOf", out _), "form body should not be wrapped in a top-level allOf");
        Assert.False(formSchema.TryGetProperty("$ref", out _), "form body should be inlined, not a $ref");
        Assert.True(formSchema.TryGetProperty("properties", out var properties), "form body must have a properties map");
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);

        var actualNames = properties.EnumerateObject()
            .Select(p => p.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expected = expectedPropertyNames
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(expected, actualNames, StringComparer.OrdinalIgnoreCase);
    }

    protected virtual string FormPropertyName(string clrPropertyName) => clrPropertyName;
}
