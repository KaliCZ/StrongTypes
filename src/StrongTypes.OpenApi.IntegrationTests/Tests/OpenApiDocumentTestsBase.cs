using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// The OpenAPI spec contract every strong-type wrapper must satisfy, regardless
/// of which generator produced the document. The Microsoft.AspNetCore.OpenApi
/// and Swashbuckle pipelines emit the same logical schema (modulo formatting,
/// ordering, and component naming); a single shared assertion suite pins the
/// shape and is run twice — once per concrete subclass — so any divergence
/// shows up as a per-generator failure.
/// </summary>
public abstract class OpenApiDocumentTestsBase(HttpClient client) : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => client.Dispose();

    protected abstract string DocumentUrl { get; }

    private async Task<JsonElement> GetDocumentAsync()
    {
        var response = await client.GetAsync(DocumentUrl, Ct);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    // Resolves a schema node against the document. Follows $ref (components
    // lookup), the single-element allOf wrapper older OpenAPI writers emit for
    // nullable $ref positions (`{ "allOf": [{ "$ref": ... }], "nullable": true }`),
    // and the oneOf wrapper the newer Microsoft.OpenApi 2.x writer emits
    // (`{ "oneOf": [{ "nullable": true }, { "$ref": ... }] }`), so the tests
    // stay focused on the underlying schema contract.
    private static JsonElement Resolve(JsonElement doc, JsonElement schema)
    {
        while (true)
        {
            if (schema.ValueKind != JsonValueKind.Object) return schema;

            if (schema.TryGetProperty("$ref", out var refProp))
            {
                var path = refProp.GetString()!;
                const string prefix = "#/components/schemas/";
                Assert.StartsWith(prefix, path);
                var name = path[prefix.Length..];
                schema = doc.GetProperty("components").GetProperty("schemas").GetProperty(name);
                continue;
            }

            if (schema.TryGetProperty("allOf", out var allOf)
                && allOf.ValueKind == JsonValueKind.Array
                && allOf.GetArrayLength() == 1)
            {
                schema = allOf[0];
                continue;
            }

            if (schema.TryGetProperty("oneOf", out var oneOf)
                && oneOf.ValueKind == JsonValueKind.Array)
            {
                JsonElement? nonNull = null;
                var allBranchesNullableOrUnderlying = true;
                foreach (var branch in oneOf.EnumerateArray())
                {
                    if (branch.ValueKind == JsonValueKind.Object
                        && branch.TryGetProperty("nullable", out var n)
                        && n.ValueKind == JsonValueKind.True
                        && branch.EnumerateObject().Count() == 1)
                    {
                        continue;
                    }

                    if (nonNull is null)
                    {
                        nonNull = branch;
                    }
                    else
                    {
                        allBranchesNullableOrUnderlying = false;
                        break;
                    }
                }

                if (allBranchesNullableOrUnderlying && nonNull is { } single)
                {
                    schema = single;
                    continue;
                }
            }

            return schema;
        }
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

    // ───────────────────────────────────────────────────────────────────
    // Nullable variants — a `NullableValue: NonEmptyString?`, a nullable
    // `Positive<int>?`, a nullable `NonEmptyEnumerable<T>?` must still expose
    // the underlying type contract. Property-level nullability is ASP.NET's
    // concern; the transformers only care about the shape of the underlying
    // schema, and this section pins that contract.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Nullable_NonEmptyString_Property_Still_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var nullableValue = Resolve(doc, Property(body, "nullableValue"));

        Assert.Equal("string", StringOrNull(nullableValue, "type"));
        Assert.Equal(1, IntOrNull(nullableValue, "minLength"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_Property_Still_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/positive-int-entities"));
        var nullableValue = Resolve(doc, Property(body, "nullableValue"));

        Assert.Equal("integer", StringOrNull(nullableValue, "type"));
        Assert.Equal("int32", StringOrNull(nullableValue, "format"));
        Assert.Equal(0m, DecimalOrNull(nullableValue, "minimum"));
        Assert.True(BoolOrFalse(nullableValue, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_NonEmptyString_Still_Renders_As_Array_With_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = Resolve(doc, Property(body, "nullableNonEmptyStringArray"));

        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = Resolve(doc, nullableArray.GetProperty("items"));
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_Positive_Int_Still_Renders_As_Array_With_Integer_Items()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = Resolve(doc, Property(body, "nullableNonEmptyPositiveIntArray"));

        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = Resolve(doc, nullableArray.GetProperty("items"));
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        Assert.Equal(0m, DecimalOrNull(items, "minimum"));
        Assert.True(BoolOrFalse(items, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Nullable_NonEmptyString_On_Dedicated_Nullables_Endpoint_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = Resolve(doc, Property(body, "nullableNonEmptyString"));

        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_On_Dedicated_Nullables_Endpoint_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = Resolve(doc, Property(body, "nullablePositiveInt"));

        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int32", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "minimum"));
        Assert.True(BoolOrFalse(value, "exclusiveMinimum"));
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

    // ───────────────────────────────────────────────────────────────────
    // Transitive composition — when one strong type wraps another, the
    // outer transformer must not swallow the inner's bounds. A
    // Maybe<Positive<int>> needs minimum:0/exclusiveMinimum:true on its
    // inner Value, not just type:integer; a Maybe<NonEmptyString> needs
    // minLength:1; a NonEmptyEnumerable<Maybe<…>> needs both the array
    // bound and the Maybe wrapper with its own inner bound.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Maybe_Of_Positive_Int_Carries_Inner_Minimum_Zero_And_ExclusiveMinimum()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybePositiveInt"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = Resolve(doc, maybe.GetProperty("properties").GetProperty("Value"));
        Assert.Equal("integer", StringOrNull(inner, "type"));
        Assert.Equal("int32", StringOrNull(inner, "format"));
        Assert.Equal(0m, DecimalOrNull(inner, "minimum"));
        Assert.True(BoolOrFalse(inner, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyString_Carries_Inner_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyString"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = Resolve(doc, maybe.GetProperty("properties").GetProperty("Value"));
        Assert.Equal("string", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minLength"));
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyEnumerable_Carries_Inner_MinItems_And_Element_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyStringArray"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = Resolve(doc, maybe.GetProperty("properties").GetProperty("Value"));
        Assert.Equal("array", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minItems"));

        var items = Resolve(doc, inner.GetProperty("items"));
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Maybe_Of_Positive_Int_Carries_Every_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/nested-strong-types"));
        var array = Resolve(doc, Property(body, "nonEmptyArrayOfMaybePositiveInt"));

        Assert.Equal("array", StringOrNull(array, "type"));
        Assert.Equal(1, IntOrNull(array, "minItems"));

        var maybe = Resolve(doc, array.GetProperty("items"));
        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = Resolve(doc, maybe.GetProperty("properties").GetProperty("Value"));
        Assert.Equal("integer", StringOrNull(inner, "type"));
        Assert.Equal("int32", StringOrNull(inner, "format"));
        Assert.Equal(0m, DecimalOrNull(inner, "minimum"));
        Assert.True(BoolOrFalse(inner, "exclusiveMinimum"));
    }
}
