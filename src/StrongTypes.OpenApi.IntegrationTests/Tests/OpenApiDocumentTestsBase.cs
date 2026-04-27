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
    // lookup) and the various wrappers the OpenAPI writers emit for nullable
    // positions:
    //   3.0  `{ "allOf": [{ "$ref": ... }], "nullable": true }`
    //   3.0  `{ "oneOf": [{ "nullable": true }, { "$ref": ... }] }`
    //   3.1  `{ "anyOf": [{ "type": "null" }, { "$ref": ... }] }`
    // so the tests stay focused on the underlying schema contract regardless
    // of which version of OpenAPI emits them.
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

            if (TryUnwrapNullableUnion(schema, "oneOf", out var unwrappedOneOf))
            {
                schema = unwrappedOneOf;
                continue;
            }

            if (TryUnwrapNullableUnion(schema, "anyOf", out var unwrappedAnyOf))
            {
                schema = unwrappedAnyOf;
                continue;
            }

            return schema;
        }
    }

    private static bool TryUnwrapNullableUnion(JsonElement schema, string keyword, out JsonElement underlying)
    {
        underlying = default;
        if (!schema.TryGetProperty(keyword, out var union) || union.ValueKind != JsonValueKind.Array)
            return false;

        JsonElement? nonNull = null;
        foreach (var branch in union.EnumerateArray())
        {
            if (branch.ValueKind != JsonValueKind.Object) return false;
            if (IsNullBranch(branch)) continue;
            if (nonNull is not null) return false;
            nonNull = branch;
        }

        if (nonNull is not { } single) return false;
        underlying = single;
        return true;
    }

    private static bool IsNullBranch(JsonElement branch)
    {
        // 3.0 form: `{ "nullable": true }`
        if (branch.TryGetProperty("nullable", out var n)
            && n.ValueKind == JsonValueKind.True
            && branch.EnumerateObject().Count() == 1)
            return true;

        // 3.1 form: `{ "type": "null" }`
        if (branch.TryGetProperty("type", out var t)
            && t.ValueKind == JsonValueKind.String
            && t.GetString() == "null"
            && branch.EnumerateObject().Count() == 1)
            return true;

        return false;
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

    // The two OpenAPI versions encode an exclusive bound differently:
    //   3.0 → `minimum: <n>` paired with `exclusiveMinimum: true` (boolean)
    //   3.1 → `exclusiveMinimum: <n>` (numeric value, no companion `minimum`)
    // The wrapper contract is "exclusive lower bound at 0" regardless; these
    // helpers normalise both encodings so the shared assertions stay version-
    // agnostic. Same for the upper-bound pair.
    private static void AssertExclusiveLowerBound(JsonElement schema, decimal expected)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMinimum", out var ex),
            "exclusiveMinimum is missing");

        if (ex.ValueKind == JsonValueKind.Number)
        {
            Assert.Equal(expected, ex.GetDecimal());
            return;
        }

        Assert.Equal(JsonValueKind.True, ex.ValueKind);
        Assert.Equal(expected, DecimalOrNull(schema, "minimum"));
    }

    private static void AssertExclusiveUpperBound(JsonElement schema, decimal expected)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMaximum", out var ex),
            "exclusiveMaximum is missing");

        if (ex.ValueKind == JsonValueKind.Number)
        {
            Assert.Equal(expected, ex.GetDecimal());
            return;
        }

        Assert.Equal(JsonValueKind.True, ex.ValueKind);
        Assert.Equal(expected, DecimalOrNull(schema, "maximum"));
    }

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
        AssertExclusiveLowerBound(value, 0m);
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
        AssertExclusiveUpperBound(value, 0m);
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
        AssertExclusiveLowerBound(items, 0m);
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
        AssertExclusiveLowerBound(nullableValue, 0m);
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
        AssertExclusiveLowerBound(items, 0m);
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
        AssertExclusiveLowerBound(value, 0m);
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
        AssertExclusiveLowerBound(inner, 0m);
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
        AssertExclusiveLowerBound(inner, 0m);
    }

    // ───────────────────────────────────────────────────────────────────
    // Annotation propagation — when a property annotated with
    // [StringLength], [RegularExpression], [Range], [MaxLength] has a
    // strong-type wrapper as its CLR type, the caller's bounds must
    // reach the wire. Without help, the generators describe the property
    // as just `{ "$ref": ".../NonEmptyString" }` and drop every
    // annotation on the floor — the very thing this whole package
    // exists to prevent.
    //
    // The property-level pass each pipeline runs is allowed to either
    // inline the merged schema on the property or layer the caller's
    // bounds via `$ref` + `allOf`. The collectors below walk both forms
    // and report the tightest applicable bound, so the assertions here
    // pin only the externally observable contract — not which encoding
    // the pipeline happens to use.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Property_NonEmptyString_With_StringLength_And_Pattern_Carries_All_Three()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-texts"));
        var username = Property(body, "username");

        Assert.Equal(3, CollectMaxInt(doc, username, "minLength"));
        Assert.Equal(50, CollectMinInt(doc, username, "maxLength"));
        Assert.Equal("^[a-zA-Z0-9_]+$", CollectFirstString(doc, username, "pattern"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_StringLength_Floors_To_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-texts"));
        var email = Property(body, "email");

        // [StringLength(254)] sets only an upper bound; the wrapper's
        // floor of 1 must remain.
        Assert.Equal(1, CollectMaxInt(doc, email, "minLength"));
        Assert.Equal(254, CollectMinInt(doc, email, "maxLength"));
        Assert.Equal("^[^@]+@[^@]+$", CollectFirstString(doc, email, "pattern"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_EmailAddress_Carries_Format_Email_And_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-texts"));
        var contactEmail = Property(body, "contactEmail");

        Assert.Equal(1, CollectMaxInt(doc, contactEmail, "minLength"));
        Assert.Equal("email", CollectFirstString(doc, contactEmail, "format"));
    }

    [Fact]
    public async Task Property_NonEmptyString_Without_Annotations_Renders_Plain_Wrapper()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-texts"));
        var description = Property(body, "description");

        Assert.Equal(1, CollectMaxInt(doc, description, "minLength"));
        Assert.Null(CollectMinInt(doc, description, "maxLength"));
        Assert.Null(CollectFirstString(doc, description, "pattern"));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Carries_Both_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-numbers"));
        var age = Property(body, "age");

        Assert.Equal(18m, CollectMaxLowerBound(doc, age));
        Assert.Equal(120m, CollectMinUpperBound(doc, age));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Across_Floor_Lets_Wrapper_Floor_Win()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-numbers"));
        var range = Property(body, "rangeAcrossFloor");

        // [Range(-5, 5)] would loosen the lower bound to -5, but the
        // wrapper's exclusiveMinimum:0 floor wins. Upper bound 5 stays.
        AssertExclusiveLowerBoundReachable(doc, range, 0m);
        Assert.Equal(5m, CollectMinUpperBound(doc, range));
    }

    // ───────────────────────────────────────────────────────────────────
    // Required-array contract — non-nullable C# properties are required,
    // nullable C# properties are not. Microsoft and Swashbuckle disagree
    // by default (Microsoft puts everything in required including
    // nullable refs; Swashbuckle puts nothing); the wrapper-paint passes
    // normalise both pipelines to the OpenAPI semantic.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Required_Reference_NonNullable_Is_Listed_Nullable_Is_Not()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var required = ReadRequiredArray(body);

        Assert.Contains("value", required);
        Assert.DoesNotContain("nullableValue", required);
    }

    [Fact]
    public async Task Required_Struct_NonNullable_Is_Listed_Nullable_Is_Not()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/positive-int-entities"));
        var required = ReadRequiredArray(body);

        Assert.Contains("value", required);
        Assert.DoesNotContain("nullableValue", required);
    }

    private static string[] ReadRequiredArray(JsonElement schema)
    {
        if (!schema.TryGetProperty("required", out var req) || req.ValueKind != JsonValueKind.Array)
            return [];
        return req.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();
    }

    [Fact]
    public async Task Property_NonEmptyEnumerable_With_MaxLength_Carries_Min_And_MaxItems()
    {
        var doc = await GetDocumentAsync();
        var body = Resolve(doc, RequestSchema(doc, "/annotated-tags"));
        var tags = Property(body, "tags");

        Assert.Equal(1, CollectMaxInt(doc, tags, "minItems"));
        Assert.Equal(10, CollectMinInt(doc, tags, "maxItems"));
    }

    // Walk a property schema across $ref, allOf, oneOf, anyOf and yield
    // each schema layer along the way (skipping the null-branches that
    // encode `T?`). The annotation-propagation assertions don't care
    // whether the pipeline inlined the merged schema or layered the
    // caller's bounds via $ref + allOf — they care that the bounds are
    // reachable somewhere in the chain.
    private static IEnumerable<JsonElement> WalkSchemaLayers(JsonElement doc, JsonElement schema)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<JsonElement>();
        queue.Enqueue(schema);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.ValueKind != JsonValueKind.Object) continue;

            yield return current;

            if (current.TryGetProperty("$ref", out var refProp))
            {
                var path = refProp.GetString()!;
                const string prefix = "#/components/schemas/";
                if (path.StartsWith(prefix) && seen.Add(path))
                {
                    var name = path[prefix.Length..];
                    queue.Enqueue(doc.GetProperty("components").GetProperty("schemas").GetProperty(name));
                }
            }

            foreach (var key in new[] { "allOf", "oneOf", "anyOf" })
            {
                if (!current.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
                foreach (var branch in arr.EnumerateArray())
                {
                    if (branch.ValueKind == JsonValueKind.Object && IsNullBranch(branch)) continue;
                    queue.Enqueue(branch);
                }
            }
        }
    }

    private static int? CollectMaxInt(JsonElement doc, JsonElement schema, string keyword)
    {
        int? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (!layer.TryGetProperty(keyword, out var v) || v.ValueKind != JsonValueKind.Number) continue;
            var i = v.GetInt32();
            if (best is null || i > best) best = i;
        }
        return best;
    }

    private static int? CollectMinInt(JsonElement doc, JsonElement schema, string keyword)
    {
        int? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (!layer.TryGetProperty(keyword, out var v) || v.ValueKind != JsonValueKind.Number) continue;
            var i = v.GetInt32();
            if (best is null || i < best) best = i;
        }
        return best;
    }

    private static string? CollectFirstString(JsonElement doc, JsonElement schema, string keyword)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (layer.TryGetProperty(keyword, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }

    private static decimal? CollectMaxLowerBound(JsonElement doc, JsonElement schema)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (TryReadInclusiveLower(layer, out var v) && (best is null || v > best)) best = v;
        }
        return best;
    }

    private static decimal? CollectMinUpperBound(JsonElement doc, JsonElement schema)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (TryReadInclusiveUpper(layer, out var v) && (best is null || v < best)) best = v;
        }
        return best;
    }

    private static bool TryReadInclusiveLower(JsonElement layer, out decimal value)
    {
        value = 0m;
        if (layer.TryGetProperty("minimum", out var min) && min.ValueKind == JsonValueKind.Number)
        {
            value = min.GetDecimal();
            return true;
        }
        return false;
    }

    private static bool TryReadInclusiveUpper(JsonElement layer, out decimal value)
    {
        value = 0m;
        if (layer.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number)
        {
            value = max.GetDecimal();
            return true;
        }
        return false;
    }

    private static void AssertExclusiveLowerBoundReachable(JsonElement doc, JsonElement schema, decimal expected)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (!layer.TryGetProperty("exclusiveMinimum", out var ex)) continue;

            if (ex.ValueKind == JsonValueKind.Number && ex.GetDecimal() == expected) return;
            if (ex.ValueKind == JsonValueKind.True
                && layer.TryGetProperty("minimum", out var min)
                && min.ValueKind == JsonValueKind.Number
                && min.GetDecimal() == expected) return;
        }

        Assert.Fail($"Expected exclusive lower bound of {expected} reachable from this schema, but none found.");
    }
}
