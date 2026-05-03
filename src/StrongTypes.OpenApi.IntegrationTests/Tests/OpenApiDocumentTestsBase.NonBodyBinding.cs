using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// Asserts the OpenAPI parameter / form-body schemas the pipelines emit for
/// strong-type parameters bound from non-body sources
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>,
/// <c>[FromForm]</c>). Required and nullable variants of each wrapped type
/// are exercised separately so a regression on either branch shows up.
///
/// Pipelines that don't yet rewrite these schemas (i.e.
/// <see cref="OpenApiDocumentTestsBase.IsNonBodyParameterStrongTypeSchemaBroken"/>
/// or the form-equivalent flags) get the underlying-type assertion path so
/// the suite documents current behaviour rather than skipping. Fixing the
/// pipeline flips the flag and turns those tests into the strong-type-aware
/// assertion path.
/// </summary>
public abstract partial class OpenApiDocumentTestsBase
{
    // ── [FromQuery] ──────────────────────────────────────────────────────

    [Fact]
    public Task FromQuery_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertNonBodyNonEmptyString("/binding-probe/query", "name");

    [Fact]
    public Task FromQuery_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertNonBodyNonEmptyString("/binding-probe/query", "nullableName");

    [Fact]
    public Task FromQuery_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertNonBodyPositiveInt("/binding-probe/query", "count");

    [Fact]
    public Task FromQuery_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertNonBodyPositiveInt("/binding-probe/query", "nullableCount");

    [Fact]
    public Task FromQuery_Email_RendersAsString_WithEmailFormat_When_NotBroken()
        => AssertNonBodyEmail("/binding-probe/query", "email");

    [Fact]
    public Task FromQuery_NullableEmail_RendersAsString_WithEmailFormat_When_NotBroken()
        => AssertNonBodyEmail("/binding-probe/query", "nullableEmail");

    // ── [FromRoute] ──────────────────────────────────────────────────────

    [Fact]
    public Task FromRoute_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertNonBodyNonEmptyString("/binding-probe/route/{name}/{count}", "name");

    [Fact]
    public Task FromRoute_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertNonBodyPositiveInt("/binding-probe/route/{name}/{count}", "count");

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public Task FromHeader_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertNonBodyNonEmptyString("/binding-probe/header", "X-Name");

    [Fact]
    public Task FromHeader_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertNonBodyNonEmptyString("/binding-probe/header", "X-Nullable-Name");

    [Fact]
    public Task FromHeader_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertNonBodyPositiveInt("/binding-probe/header", "X-Count");

    [Fact]
    public Task FromHeader_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertNonBodyPositiveInt("/binding-probe/header", "X-Nullable-Count");

    // ── [FromForm] ───────────────────────────────────────────────────────

    [Fact]
    public Task FromForm_NonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertFormPropertyNonEmptyString("name");

    [Fact]
    public Task FromForm_NullableNonEmptyString_RendersAsString_WithMinLength_When_NotBroken()
        => AssertFormPropertyNonEmptyString("nullableName");

    [Fact]
    public Task FromForm_PositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertFormPropertyPositiveInt("count");

    [Fact]
    public Task FromForm_NullablePositiveInt_RendersAsExclusivePositive_When_NotBroken()
        => AssertFormPropertyPositiveInt("nullableCount");

    [Fact]
    public Task FromForm_Email_RendersAsString_WithEmailFormat_When_NotBroken()
        => AssertFormPropertyEmail("email");

    [Fact]
    public Task FromForm_NullableEmail_RendersAsString_WithEmailFormat_When_NotBroken()
        => AssertFormPropertyEmail("nullableEmail");

    // ── Shared assertion machinery ────────────────────────────────────────

    private async Task AssertNonBodyNonEmptyString(string path, string parameter)
    {
        var schema = ParameterSchema(await GetDocumentAsync(), path, parameter);
        if (IsNonBodyParameterStrongTypeSchemaBroken)
        {
            // Pipeline emits an underlying-type schema with no strong-type
            // constraints. Don't lock down its exact shape — different
            // sources (query, header, route) produce different "wrong"
            // shapes; just confirm the parameter slot is wired up.
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    private async Task AssertNonBodyPositiveInt(string path, string parameter)
    {
        var schema = ParameterSchema(await GetDocumentAsync(), path, parameter);
        if (IsNonBodyParameterStrongTypeSchemaBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, Version);
    }

    private async Task AssertNonBodyEmail(string path, string parameter)
    {
        var schema = ParameterSchema(await GetDocumentAsync(), path, parameter);
        if (IsNonBodyParameterStrongTypeSchemaBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
        Assert.Equal(254, IntOrNull(schema, "maxLength"));
        if (!IsEmailStringFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }

    private async Task<JsonElement?> TryGetFormProperty(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var formSchema = FormRequestSchema(doc, "/binding-probe/form");
        if (IsFormPropertiesSchemaBroken)
        {
            // The pipeline emitted an allOf-of-property-schemas with the
            // names dropped; we can't navigate to a specific property by
            // name. Just confirm the form body schema is there at all.
            Assert.Equal(JsonValueKind.Object, formSchema.ValueKind);
            return null;
        }
        var properties = formSchema.GetProperty("properties");
        // Pipelines disagree on casing — Microsoft emits PascalCase, Swashbuckle camelCase.
        // Both are correct OpenAPI; match either so tests aren't pipeline-flavoured.
        foreach (var entry in properties.EnumerateObject())
        {
            if (string.Equals(entry.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        Assert.Fail($"form schema has no '{propertyName}' property (case-insensitive)");
        return default;
    }

    private async Task AssertFormPropertyNonEmptyString(string propertyName)
    {
        var prop = await TryGetFormProperty(propertyName);
        if (prop is not { } schema) return;
        if (IsFormPropertyStrongTypeSchemaBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    private async Task AssertFormPropertyPositiveInt(string propertyName)
    {
        var prop = await TryGetFormProperty(propertyName);
        if (prop is not { } schema) return;
        if (IsFormPropertyStrongTypeSchemaBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, Version);
    }

    private async Task AssertFormPropertyEmail(string propertyName)
    {
        var prop = await TryGetFormProperty(propertyName);
        if (prop is not { } schema) return;
        if (IsFormPropertyStrongTypeSchemaBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
        Assert.Equal(254, IntOrNull(schema, "maxLength"));
        if (!IsEmailStringFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }
}
