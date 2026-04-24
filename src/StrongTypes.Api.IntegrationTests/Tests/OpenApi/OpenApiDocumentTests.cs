using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests.OpenApi;

/// <summary>
/// Fetches the OpenAPI document served by the API and asserts that every
/// strong type is described using the JSON shape its converter actually reads
/// and writes — not the raw CLR shape ASP.NET Core would infer by default.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OpenApiDocumentTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    private async Task<JsonElement> GetDocumentAsync()
    {
        var response = await _client.GetAsync("/openapi/v1.json", Ct);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    // Resolves a (possibly $ref) schema node against the document's components/schemas map.
    private static JsonElement Resolve(JsonElement doc, JsonElement schema)
    {
        if (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("$ref", out var refProp))
        {
            var path = refProp.GetString()!;
            // e.g. "#/components/schemas/NonEmptyString"
            const string prefix = "#/components/schemas/";
            Assert.StartsWith(prefix, path);
            var name = path[prefix.Length..];
            return doc.GetProperty("components").GetProperty("schemas").GetProperty(name);
        }

        return schema;
    }

    private static JsonElement RequestSchema(JsonElement doc, string path, string method = "post")
        => doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");

    private static JsonElement Property(JsonElement schema, string propertyName)
        => schema.GetProperty("properties").GetProperty(propertyName);

    private static string? StringOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static int? IntOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : null;

    private static decimal? DecimalOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal()
            : null;

    private static bool BoolOrFalse(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.True;

    [Fact]
    public async Task Document_IsServed()
    {
        var doc = await GetDocumentAsync();
        Assert.Equal(JsonValueKind.Object, doc.ValueKind);
        Assert.True(doc.TryGetProperty("paths", out _));
    }

    [Fact]
    public async Task NonEmptyString_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var value = Resolve(doc, Property(body, "value"));

        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
        Assert.False(value.TryGetProperty("properties", out _));
    }

    [Fact]
    public async Task Positive_Int_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/positive-int-entities"));
        var value = Resolve(doc, Property(body, "value"));

        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int32", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "minimum"));
        Assert.True(BoolOrFalse(value, "exclusiveMinimum"));
    }

    [Fact]
    public async Task NonNegative_Long_Renders_As_Integer_Int64_With_Minimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/non-negative-long-entities"));
        var value = Resolve(doc, Property(body, "value"));

        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int64", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "minimum"));
        Assert.False(BoolOrFalse(value, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Negative_Double_Renders_As_Number_Double_With_ExclusiveMaximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/negative-double-entities"));
        var value = Resolve(doc, Property(body, "value"));

        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal("double", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "maximum"));
        Assert.True(BoolOrFalse(value, "exclusiveMaximum"));
    }

    [Fact]
    public async Task NonPositive_Decimal_Renders_With_Maximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/non-positive-decimal-entities"));
        var value = Resolve(doc, Property(body, "value"));

        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal(0m, DecimalOrNull(value, "maximum"));
        Assert.False(BoolOrFalse(value, "exclusiveMaximum"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Renders_As_Array_With_MinItems_And_Items_Schema()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var nonEmpty = Resolve(doc, Property(body, "nonEmpty"));

        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = Resolve(doc, nonEmpty.GetProperty("items"));
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Enumerable_Of_NonEmptyString_Has_No_MinItems_But_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var enumerable = Resolve(doc, Property(body, "enumerable"));

        Assert.Equal("array", StringOrNull(enumerable, "type"));
        Assert.Null(IntOrNull(enumerable, "minItems"));

        var items = Resolve(doc, enumerable.GetProperty("items"));
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Positive_Int_Composes_With_Numeric_Transformer()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/collections/positive-int"));
        var nonEmpty = Resolve(doc, Property(body, "nonEmpty"));

        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = Resolve(doc, nonEmpty.GetProperty("items"));
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        Assert.Equal(0m, DecimalOrNull(items, "minimum"));
        Assert.True(BoolOrFalse(items, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Maybe_T_Renders_As_Wrapper_Object_With_Value_Property()
    {
        // The PATCH request for a numeric entity carries `Maybe<T>? NullableValue`.
        // The converter writes {"Value": x} or {"Value": null} and reads {} as None,
        // so the schema must describe an object with a non-required Value property.
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/positive-int-entities/{id}", method: "patch"));
        var nullableValue = Resolve(doc, Property(body, "nullableValue"));

        Assert.Equal("object", StringOrNull(nullableValue, "type"));
        Assert.True(nullableValue.TryGetProperty("properties", out var props));
        Assert.True(props.TryGetProperty("Value", out var inner));

        var innerSchema = Resolve(doc, inner);
        Assert.Equal("integer", StringOrNull(innerSchema, "type"));

        // Value is not listed under `required` — that's how the converter
        // accepts `{}` as the None case.
        var required = nullableValue.TryGetProperty("required", out var r)
            ? r.EnumerateArray().Select(e => e.GetString()).ToArray()
            : [];
        Assert.DoesNotContain("Value", required);
    }
}
