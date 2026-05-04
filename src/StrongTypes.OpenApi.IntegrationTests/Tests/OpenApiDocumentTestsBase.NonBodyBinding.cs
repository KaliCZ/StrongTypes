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
///
/// Pipelines that don't run the strong-type schema transformer on these
/// slots (i.e. <see cref="OpenApiDocumentTestsBase.IsNonJsonBodyStrongTypeSchemaBroken"/>)
/// or that emit the form schema as <c>allOf</c> with field names dropped
/// (<see cref="OpenApiDocumentTestsBase.IsFormPropertiesSchemaBroken"/>)
/// take the broken-shape assertion path. Fixing the pipeline flips the
/// flag and turns those tests into the strong-type-aware assertion path.
/// </summary>
public abstract partial class OpenApiDocumentTestsBase
{
    // ── [FromQuery] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromQuery_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "name");
        AssertNonBodyNonEmptyString(schema, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromQuery_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableName");
        AssertNonBodyNonEmptyString(schema, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromQuery_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "count");
        AssertNonBodyPositiveInt(schema, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    [Fact]
    public async Task FromQuery_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableCount");
        AssertNonBodyPositiveInt(schema, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    [Fact]
    public async Task FromQuery_Email_RendersAsString_WithEmailFormat_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "email");
        AssertNonBodyEmail(schema, IsNonJsonBodyStrongTypeSchemaBroken, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromQuery_NullableEmail_RendersAsString_WithEmailFormat_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/query", "nullableEmail");
        AssertNonBodyEmail(schema, IsNonJsonBodyStrongTypeSchemaBroken, IsEmailStringFormatBroken);
    }

    // ── [FromRoute] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromRoute_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "name");
        AssertNonBodyNonEmptyString(schema, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromRoute_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/route/{name}/{count}", "count");
        AssertNonBodyPositiveInt(schema, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public async Task FromHeader_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Name");
        AssertNonBodyNonEmptyString(schema, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromHeader_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Name");
        AssertNonBodyNonEmptyString(schema, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromHeader_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Count");
        AssertNonBodyPositiveInt(schema, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    [Fact]
    public async Task FromHeader_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var schema = ParameterSchema(await GetDocumentAsync(), "/binding-probe/header", "X-Nullable-Count");
        AssertNonBodyPositiveInt(schema, IsNonJsonBodyStrongTypeSchemaBroken, Version);
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
    public async Task FromForm_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyString(formSchema, "name", allOfIndex: 0, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromForm_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyNonEmptyString(formSchema, "nullableName", allOfIndex: 1, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken);
    }

    [Fact]
    public async Task FromForm_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveInt(formSchema, "count", allOfIndex: 2, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyPositiveInt(formSchema, "nullableCount", allOfIndex: 3, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken, Version);
    }

    [Fact]
    public async Task FromForm_Email_RendersAsString_WithEmailFormat_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmail(formSchema, "email", allOfIndex: 4, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken, IsEmailStringFormatBroken);
    }

    [Fact]
    public async Task FromForm_NullableEmail_RendersAsString_WithEmailFormat_When_NotBroken()
    {
        var formSchema = FormRequestSchema(await GetDocumentAsync(), "/binding-probe/form");
        AssertFormPropertyEmail(formSchema, "nullableEmail", allOfIndex: 5, IsFormPropertiesSchemaBroken, IsNonJsonBodyStrongTypeSchemaBroken, IsEmailStringFormatBroken);
    }
}
