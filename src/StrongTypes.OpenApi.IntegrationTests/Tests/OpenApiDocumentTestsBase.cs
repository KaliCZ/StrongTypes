using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ComponentSchemas;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

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

    protected enum OpenApiVersion { V3_0, V3_1 }

    /// <summary>
    /// The OpenAPI spec version the pipeline under test emits. Exclusive-bound
    /// helpers below pin the version's encoding strictly: 3.0 must emit
    /// <c>minimum: &lt;n&gt;</c> + <c>exclusiveMinimum: true</c> (boolean
    /// pair), 3.1 must emit <c>exclusiveMinimum: &lt;n&gt;</c> (numeric).
    /// Cross-version output fails the test rather than silently passing.
    /// </summary>
    protected abstract OpenApiVersion Version { get; }

    // Per-feature flags below name a specific keyword the underlying
    // pipeline doesn't natively write for the matching annotation, even
    // on a primitive-typed property. The corresponding tests run on every
    // pipeline; when the flag is set, they assert the keyword is *absent*
    // rather than skipping — so the suite documents the framework's
    // actual behaviour and catches it if the framework starts honouring
    // the annotation. Each flag is set in the per-pipeline subclass.
    protected virtual bool IsEmailStringFormatBroken => false;
    protected virtual bool IsLengthAttributeBroken => false;
    protected virtual bool IsBase64StringFormatBroken => false;
    protected virtual bool IsDescriptionAttributeBroken => false;
    protected virtual bool IsExclusiveRangeBroken => false;

    private async Task<JsonElement> GetDocumentAsync()
    {
        var response = await client.GetAsync(DocumentUrl, Ct);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private JsonElement UnwrapNullableUnion(JsonElement schema, string keyword)
    {
        Assert.True(schema.TryGetProperty(keyword, out var union), $"{keyword} is missing");
        Assert.Equal(JsonValueKind.Array, union.ValueKind);

        JsonElement? nonNull = null;
        foreach (var branch in union.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Object, branch.ValueKind);
            if (IsNullBranch(branch)) continue;
            Assert.True(nonNull is null, $"{keyword} must have exactly one non-null branch");
            nonNull = branch;
        }

        Assert.True(nonNull.HasValue, $"{keyword} has no non-null branch");
        return nonNull.Value;
    }

    // Version-strict unwrap for a property whose CLR type is nullable. Each
    // pipeline emits a different wrapper shape (oneOf+nullable:true,
    // anyOf with {type:"null"}, allOf, or even no wrapper at all when the
    // pipeline relies solely on `required` to mark optionality), but the
    // 3.0/3.1 markers are strictly partitioned:
    //   - 3.0 must NOT carry {"type":"null"} branches anywhere (3.1 marker)
    //   - 3.1 must NOT carry the nullable:true keyword (3.0 marker)
    // Cross-version output fails the assertion. After the version-marker
    // check, this method walks past the wrapper layer (whichever form the
    // pipeline used) and returns the inner schema for downstream
    // navigation/assertion.
    protected JsonElement UnwrapNullableProperty(JsonElement schema)
    {
        AssertVersionMarkers(schema);

        if (schema.TryGetProperty("oneOf", out var oneOf) && oneOf.ValueKind == JsonValueKind.Array)
            return UnwrapNullableUnion(schema, "oneOf");
        if (schema.TryGetProperty("anyOf", out var anyOf) && anyOf.ValueKind == JsonValueKind.Array)
            return UnwrapNullableUnion(schema, "anyOf");
        if (schema.TryGetProperty("allOf", out var allOf) && allOf.ValueKind == JsonValueKind.Array
            && allOf.GetArrayLength() == 1)
            return allOf[0];

        // No nullable wrapper layer — pipeline encoded the property's
        // nullability solely via `required` (Swashbuckle's typical form for
        // value-typed and same-shape-as-non-nullable cases). The schema is
        // already the leaf; return it.
        return schema;
    }

    private void AssertVersionMarkers(JsonElement schema)
    {
        if (Version == OpenApiVersion.V3_1)
        {
            Assert.False(
                schema.TryGetProperty("nullable", out _),
                "3.1 schemas must not use the nullable:true marker (3.0 form)");
            return;
        }
        // V3_0: forbid {"type":"null"} branches in any union.
        foreach (var key in new[] { "oneOf", "anyOf" })
        {
            if (!schema.TryGetProperty(key, out var union) || union.ValueKind != JsonValueKind.Array) continue;
            foreach (var branch in union.EnumerateArray())
            {
                Assert.False(
                    IsNullBranch3_1(branch),
                    $"3.0 schemas must not contain a {{\"type\":\"null\"}} branch ({key}); that's the 3.1 form");
            }
        }
    }

    /// <summary>
    /// True iff <paramref name="branch"/> is the OpenAPI 3.0 null marker:
    /// a singleton object <c>{ "nullable": true }</c> with no other
    /// keywords. Pins the branch as encoding "or null" inside a
    /// <c>oneOf</c>/<c>anyOf</c> union; the singleton check excludes
    /// nullable schemas that also carry their own constraints.
    /// </summary>
    private static bool IsNullBranch3_0(JsonElement branch) =>
        branch.TryGetProperty("nullable", out var n)
        && n.ValueKind == JsonValueKind.True
        && branch.EnumerateObject().Count() == 1;

    /// <summary>
    /// True iff <paramref name="branch"/> is the OpenAPI 3.1 null marker:
    /// a singleton object <c>{ "type": "null" }</c> with no other
    /// keywords. The 3.1 spec dropped the <c>nullable</c> keyword and
    /// uses a <c>null</c>-typed branch instead.
    /// </summary>
    private static bool IsNullBranch3_1(JsonElement branch) =>
        branch.TryGetProperty("type", out var t)
        && t.ValueKind == JsonValueKind.String
        && t.GetString() == "null"
        && branch.EnumerateObject().Count() == 1;

    /// <summary>
    /// True iff <paramref name="branch"/> is the null marker for the
    /// document's declared OpenAPI version. Dispatches strictly: a 3.0
    /// document accepts only the 3.0 marker, a 3.1 document accepts
    /// only the 3.1 marker, so cross-version contamination surfaces as
    /// an unhandled branch rather than silently passing through.
    /// </summary>
    private bool IsNullBranch(JsonElement branch) =>
        Version == OpenApiVersion.V3_1 ? IsNullBranch3_1(branch) : IsNullBranch3_0(branch);

    // The two OpenAPI versions encode an exclusive bound differently:
    //   3.0 → `minimum: <n>` paired with `exclusiveMinimum: true` (boolean)
    //   3.1 → `exclusiveMinimum: <n>` (numeric, no companion `minimum`)
    // The helpers below pin the encoding for the version under test; an
    // emission in the other version's form fails the assertion.
    private void AssertExclusiveLowerBound(JsonElement schema, decimal expected)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMinimum", out var ex),
            "exclusiveMinimum is missing");

        if (Version == OpenApiVersion.V3_1)
        {
            Assert.Equal(JsonValueKind.Number, ex.ValueKind);
            Assert.Equal(expected, ex.GetDecimal());
        }
        else
        {
            Assert.Equal(JsonValueKind.True, ex.ValueKind);
            Assert.Equal(expected, DecimalOrNull(schema, "minimum"));
        }
    }

    private void AssertExclusiveUpperBound(JsonElement schema, decimal expected)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMaximum", out var ex),
            "exclusiveMaximum is missing");

        if (Version == OpenApiVersion.V3_1)
        {
            Assert.Equal(JsonValueKind.Number, ex.ValueKind);
            Assert.Equal(expected, ex.GetDecimal());
        }
        else
        {
            Assert.Equal(JsonValueKind.True, ex.ValueKind);
            Assert.Equal(expected, DecimalOrNull(schema, "maximum"));
        }
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
        var body = FollowRef(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
        Assert.False(value.TryGetProperty("properties", out _));
    }

    [Fact]
    public async Task Positive_Int_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int32", StringOrNull(value, "format"));
        AssertExclusiveLowerBound(value, 0m);
    }

    [Fact]
    public async Task NonNegative_Long_Renders_As_Integer_Int64_With_Minimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-negative-long-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int64", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "minimum"));
        Assert.False(BoolOrFalse(value, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Negative_Double_Renders_As_Number_Double_With_ExclusiveMaximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/negative-double-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal("double", StringOrNull(value, "format"));
        AssertExclusiveUpperBound(value, 0m);
    }

    [Fact]
    public async Task NonPositive_Decimal_Renders_With_Maximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-positive-decimal-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal(0m, DecimalOrNull(value, "maximum"));
        Assert.False(BoolOrFalse(value, "exclusiveMaximum"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Renders_As_Array_With_MinItems_And_Items_Schema()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var nonEmpty = Property(body, "nonEmpty");

        AssertInlineSchema(nonEmpty);
        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = nonEmpty.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Enumerable_Of_NonEmptyString_Has_No_MinItems_But_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var enumerable = Property(body, "enumerable");

        AssertInlineSchema(enumerable);
        Assert.Equal("array", StringOrNull(enumerable, "type"));
        Assert.Null(IntOrNull(enumerable, "minItems"));

        var items = enumerable.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Positive_Int_Composes_With_Numeric_Transformer()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/positive-int"));
        var nonEmpty = Property(body, "nonEmpty");

        AssertInlineSchema(nonEmpty);
        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = nonEmpty.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m);
    }

    // ───────────────────────────────────────────────────────────────────
    // Collection shapes — every CLR collection shape carrying a strong-type
    // element must expose `items` with the element's wire schema. The
    // `/collections/shapes` endpoint declares one property per shape, all
    // typed as `Positive<int>` so the items schema must carry
    // `exclusiveMinimum: 0`. Each shape is asserted independently so the
    // failure surface tells us which shapes the underlying pipeline (and
    // any items-backfill transformer it relies on) actually covers.
    // ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("asEnumerable")]
    [InlineData("asIList")]
    [InlineData("asIReadOnlyList")]
    [InlineData("asList")]
    [InlineData("asArray")]
    [InlineData("asNonEmpty")]
    public async Task Collection_Shape_Of_Positive_Int_Renders_As_Array_With_Integer_Items(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/shapes"));
        var array = Property(body, propertyName);

        AssertInlineSchema(array);
        Assert.Equal("array", StringOrNull(array, "type"));

        var items = array.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m);
    }

    // ───────────────────────────────────────────────────────────────────
    // Dictionary shapes — the wire form for a CLR dictionary keyed by a
    // primitive is an OpenAPI object with `additionalProperties`. Each
    // dictionary property below carries a `Positive<int>` value, so the
    // value-schema position must encode `exclusiveMinimum: 0`.
    // ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("asIDictionary")]
    [InlineData("asDictionaryIntKey")]
    [InlineData("asIReadOnlyDictionary")]
    public async Task Dictionary_Shape_Of_Positive_Int_Renders_With_Integer_AdditionalProperties(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/dictionary-shapes"));
        var dict = Property(body, propertyName);

        AssertInlineSchema(dict);
        Assert.Equal("object", StringOrNull(dict, "type"));

        Assert.True(dict.TryGetProperty("additionalProperties", out var values),
            "additionalProperties is missing on the dictionary schema");
        AssertInlineSchema(values);
        Assert.Equal("integer", StringOrNull(values, "type"));
        Assert.Equal("int32", StringOrNull(values, "format"));
        AssertExclusiveLowerBound(values, 0m);
    }

    // ───────────────────────────────────────────────────────────────────
    // Modern collection shapes — FrozenSet<T>, FrozenDictionary<K,V>,
    // SortedList<K,V> are recent BCL additions whose serializer mappings
    // are array (set) and object/additionalProperties (dictionary). The
    // strong-typed value position must reach the wire schema in each.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FrozenSet_Of_Positive_Int_Renders_As_Array_With_Integer_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/modern-collections"));
        var array = Property(body, "asFrozenSet");

        AssertInlineSchema(array);
        Assert.Equal("array", StringOrNull(array, "type"));

        var items = array.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m);
    }

    [Theory]
    [InlineData("asFrozenDictionary")]
    [InlineData("asSortedList")]
    public async Task Modern_Dictionary_Shape_Of_Positive_Int_Renders_With_Integer_AdditionalProperties(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/modern-collections"));
        var dict = Property(body, propertyName);

        AssertInlineSchema(dict);
        Assert.Equal("object", StringOrNull(dict, "type"));

        Assert.True(dict.TryGetProperty("additionalProperties", out var values),
            "additionalProperties is missing on the dictionary schema");
        AssertInlineSchema(values);
        Assert.Equal("integer", StringOrNull(values, "type"));
        Assert.Equal("int32", StringOrNull(values, "format"));
        AssertExclusiveLowerBound(values, 0m);
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
        var body = FollowRef(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var nullableValue = UnwrapNullableProperty(Property(body, "nullableValue"));

        AssertInlineSchema(nullableValue);
        Assert.Equal("string", StringOrNull(nullableValue, "type"));
        Assert.Equal(1, IntOrNull(nullableValue, "minLength"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_Property_Still_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        var nullableValue = UnwrapNullableProperty(Property(body, "nullableValue"));

        AssertInlineSchema(nullableValue);
        Assert.Equal("integer", StringOrNull(nullableValue, "type"));
        Assert.Equal("int32", StringOrNull(nullableValue, "format"));
        AssertExclusiveLowerBound(nullableValue, 0m);
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_NonEmptyString_Still_Renders_As_Array_With_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = UnwrapNullableProperty(Property(body, "nullableNonEmptyStringArray"));

        AssertInlineSchema(nullableArray);
        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = nullableArray.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_Positive_Int_Still_Renders_As_Array_With_Integer_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = UnwrapNullableProperty(Property(body, "nullableNonEmptyPositiveIntArray"));

        AssertInlineSchema(nullableArray);
        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = nullableArray.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m);
    }

    [Fact]
    public async Task Nullable_NonEmptyString_On_Dedicated_Nullables_Endpoint_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = UnwrapNullableProperty(Property(body, "nullableNonEmptyString"));

        AssertInlineSchema(value);
        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_On_Dedicated_Nullables_Endpoint_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = UnwrapNullableProperty(Property(body, "nullablePositiveInt"));

        AssertInlineSchema(value);
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
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities/{id}", method: "patch"));
        var nullableValue = Resolve(doc, UnwrapNullableProperty(Property(body, "nullableValue")));

        Assert.Equal("object", StringOrNull(nullableValue, "type"));
        Assert.True(nullableValue.TryGetProperty("properties", out var props));
        Assert.True(props.TryGetProperty("Value", out var inner));

        AssertInlineSchema(inner);
        Assert.Equal("integer", StringOrNull(inner, "type"));

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
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybePositiveInt"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("integer", StringOrNull(inner, "type"));
        Assert.Equal("int32", StringOrNull(inner, "format"));
        AssertExclusiveLowerBound(inner, 0m);
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyString_Carries_Inner_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyString"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("string", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minLength"));
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyEnumerable_Carries_Inner_MinItems_And_Element_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyStringArray"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("array", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minItems"));

        var items = inner.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Maybe_Of_Positive_Int_Carries_Every_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var array = Property(body, "nonEmptyArrayOfMaybePositiveInt");

        AssertInlineSchema(array);
        Assert.Equal("array", StringOrNull(array, "type"));
        Assert.Equal(1, IntOrNull(array, "minItems"));

        var maybe = Resolve(doc, array.GetProperty("items"));
        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
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
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var username = Property(body, "username");

        Assert.Equal(3, CollectMaxInt(doc, username, "minLength"));
        Assert.Equal(50, CollectMinInt(doc, username, "maxLength"));
        Assert.Equal("^[a-zA-Z0-9_]+$", CollectFirstString(doc, username, "pattern"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_StringLength_Floors_To_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var email = Property(body, "email");

        // [StringLength(254)] sets only an upper bound; the wrapper's
        // floor of 1 must remain.
        Assert.Equal(1, CollectMaxInt(doc, email, "minLength"));
        Assert.Equal(254, CollectMinInt(doc, email, "maxLength"));
        Assert.Equal("^[^@]+@[^@]+$", CollectFirstString(doc, email, "pattern"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_EmailAddress_Carries_Wrapper_MinLength_And_Format_Email_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var contactEmail = Property(body, "contactEmail");

        // Wrapper's minLength: 1 always reaches the wire. The [EmailAddress]
        // format keyword only reaches it on pipelines that honour the
        // attribute on plain primitives.
        Assert.Equal(1, CollectMaxInt(doc, contactEmail, "minLength"));
        Assert.Equal(IsEmailStringFormatBroken ? null : "email", CollectFirstString(doc, contactEmail, "format"));
    }

    [Fact]
    public async Task Property_NonEmptyString_Without_Annotations_Renders_Plain_Wrapper()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var description = Property(body, "description");

        Assert.Equal(1, CollectMaxInt(doc, description, "minLength"));
        Assert.Null(CollectMinInt(doc, description, "maxLength"));
        Assert.Null(CollectFirstString(doc, description, "pattern"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Url_Carries_Format_Uri_And_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var websiteUrl = Property(body, "websiteUrl");

        Assert.Equal(1, CollectMaxInt(doc, websiteUrl, "minLength"));
        Assert.Equal("uri", CollectFirstString(doc, websiteUrl, "format"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Length_Tightens_Both_Bounds_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var slug = Property(body, "slug");

        // [Length(2, 8)] tightens both bounds when honoured. When dropped,
        // only the wrapper's own minLength: 1 floor reaches the wire and
        // there is no maxLength.
        Assert.Equal(IsLengthAttributeBroken ? 1 : 2, CollectMaxInt(doc, slug, "minLength"));
        Assert.Equal(IsLengthAttributeBroken ? null : 8, CollectMinInt(doc, slug, "maxLength"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Base64String_Carries_Wrapper_MinLength_And_Format_Byte_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var encodedBlob = Property(body, "encodedBlob");

        Assert.Equal(1, CollectMaxInt(doc, encodedBlob, "minLength"));
        Assert.Equal(IsBase64StringFormatBroken ? null : "byte", CollectFirstString(doc, encodedBlob, "format"));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Description_Carries_Description_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var tagline = Property(body, "tagline");

        Assert.Equal(IsDescriptionAttributeBroken ? null : "Short user tagline", CollectFirstString(doc, tagline, "description"));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Carries_Both_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var age = Property(body, "age");

        Assert.Equal(18m, CollectMaxLowerBound(doc, age));
        Assert.Equal(120m, CollectMinUpperBound(doc, age));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_MinimumIsExclusive_Carries_Exclusive_Lower_Bound_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var exclusive = Property(body, "exclusiveLowerAge");

        // [Range(1, 10, MinimumIsExclusive = true)] degrades to plain
        // [Range(1, 10)] when MinimumIsExclusive is dropped — minimum
        // becomes inclusive 1.
        Assert.Equal(10m, CollectMinUpperBound(doc, exclusive));
        if (IsExclusiveRangeBroken)
            Assert.Equal(1m, CollectMaxLowerBound(doc, exclusive));
        else
            AssertExclusiveLowerBoundReachable(doc, exclusive, 1m);
    }

    // The primitive-typed siblings below pin the same wire keywords on a
    // plain `string` / `int` / `string[]` carrying the same annotations.
    // They serve as a baseline: each pipeline's underlying annotation
    // handling must natively produce these keywords on a primitive-typed
    // property for the matching wrapper-typed assertion above to be
    // meaningful. If a primitive-typed test fails, the failure is in the
    // pipeline's own annotation-mapping step, not in our wrapper paint.

    [Fact]
    public async Task Property_String_With_StringLength_And_Pattern_Carries_All_Three()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var usernameRaw = Property(body, "usernameRaw");

        Assert.Equal(3, CollectMaxInt(doc, usernameRaw, "minLength"));
        Assert.Equal(50, CollectMinInt(doc, usernameRaw, "maxLength"));
        Assert.Equal("^[a-zA-Z0-9_]+$", CollectFirstString(doc, usernameRaw, "pattern"));
    }

    [Fact]
    public async Task Property_String_With_StringLength_Only_Carries_MaxLength_And_Pattern()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var emailRaw = Property(body, "emailRaw");

        // [StringLength(254)] sets only an upper bound; both pipelines write
        // minLength: 0 verbatim from the attribute. The wrapper sibling
        // floors that to its own minLength: 1 (asserted on the wrapper test).
        Assert.Equal(0, CollectMaxInt(doc, emailRaw, "minLength"));
        Assert.Equal(254, CollectMinInt(doc, emailRaw, "maxLength"));
        Assert.Equal("^[^@]+@[^@]+$", CollectFirstString(doc, emailRaw, "pattern"));
    }

    [Fact]
    public async Task Property_String_With_EmailAddress_Carries_Format_Email_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var contactEmailRaw = Property(body, "contactEmailRaw");

        Assert.Equal(IsEmailStringFormatBroken ? null : "email", CollectFirstString(doc, contactEmailRaw, "format"));
    }

    [Fact]
    public async Task Property_String_With_Url_Carries_Format_Uri()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var websiteUrlRaw = Property(body, "websiteUrlRaw");

        Assert.Equal("uri", CollectFirstString(doc, websiteUrlRaw, "format"));
    }

    [Fact]
    public async Task Property_String_With_Length_Tightens_Both_Bounds_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var slugRaw = Property(body, "slugRaw");

        Assert.Equal(IsLengthAttributeBroken ? null : 2, CollectMaxInt(doc, slugRaw, "minLength"));
        Assert.Equal(IsLengthAttributeBroken ? null : 8, CollectMinInt(doc, slugRaw, "maxLength"));
    }

    [Fact]
    public async Task Property_String_With_Base64String_Carries_Format_Byte_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var encodedBlobRaw = Property(body, "encodedBlobRaw");

        Assert.Equal(IsBase64StringFormatBroken ? null : "byte", CollectFirstString(doc, encodedBlobRaw, "format"));
    }

    [Fact]
    public async Task Property_String_With_Description_Carries_Description_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var taglineRaw = Property(body, "taglineRaw");

        Assert.Equal(IsDescriptionAttributeBroken ? null : "Short user tagline", CollectFirstString(doc, taglineRaw, "description"));
    }

    [Fact]
    public async Task Property_Int_With_Range_Carries_Both_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var ageRaw = Property(body, "ageRaw");

        Assert.Equal(18m, CollectMaxLowerBound(doc, ageRaw));
        Assert.Equal(120m, CollectMinUpperBound(doc, ageRaw));
    }

    [Fact]
    public async Task Property_Int_With_Range_MinimumIsExclusive_Carries_Exclusive_Lower_Bound_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var exclusiveRaw = Property(body, "exclusiveLowerAgeRaw");

        Assert.Equal(10m, CollectMinUpperBound(doc, exclusiveRaw));
        if (IsExclusiveRangeBroken)
            Assert.Equal(1m, CollectMaxLowerBound(doc, exclusiveRaw));
        else
            AssertExclusiveLowerBoundReachable(doc, exclusiveRaw, 1m);
    }

    [Fact]
    public async Task Property_StringArray_With_MaxLength_Carries_MaxItems()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-tags"));
        var tagsRaw = Property(body, "tagsRaw");

        Assert.Equal(10, CollectMinInt(doc, tagsRaw, "maxItems"));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Across_Floor_Lets_Wrapper_Floor_Win()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var range = Property(body, "rangeAcrossFloor");

        // [Range(-5, 5)] would loosen the lower bound to -5, but the
        // wrapper's exclusiveMinimum:0 floor wins. Upper bound 5 stays.
        AssertExclusiveLowerBoundReachable(doc, range, 0m);
        Assert.Equal(5m, CollectMinUpperBound(doc, range));
    }

    // ───────────────────────────────────────────────────────────────────
    // Required-array contract — strong-type wrapper properties land in
    // the `required` array iff their primitive equivalent does. Whatever
    // the underlying pipeline produces for `string`, `int`, `string?`,
    // `[Required] string`, `required string` etc. is what it must produce
    // for the matching `NonEmptyString` / `Positive<int>` property.
    // ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("plain",            "plainRaw")]
    [InlineData("nullable",         "nullableRaw")]
    [InlineData("withAttribute",    "withAttributeRaw")]
    [InlineData("attributeNullable","attributeNullableRaw")]
    [InlineData("withKeyword",      "withKeywordRaw")]
    [InlineData("keywordNullable",  "keywordNullableRaw")]
    public async Task Required_Membership_Matches_Underlying_Primitive(string strongName, string rawName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/required-variants"));
        var required = ReadRequiredArray(body);

        Assert.Equal(required.Contains(rawName), required.Contains(strongName));
    }

    // ───────────────────────────────────────────────────────────────────
    // Components cleanup — every inlineable wrapper (NonEmptyString and
    // the numeric/array generics) must be removed from
    // components.schemas after the inliner runs. Maybe<T> is the one
    // intentional exception: its object-shaped wire form is worth
    // keeping as a named component.
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Inlineable_Wrapper_Components_Are_Removed_From_Components_Schemas()
    {
        var doc = await GetDocumentAsync();
        var schemaNames = ReadComponentSchemaNames(doc);

        Assert.DoesNotContain("NonEmptyString", schemaNames);

        // Microsoft prefix style and Swashbuckle suffix style: any name
        // that begins or ends with one of the wrapper roots must be gone.
        foreach (var name in schemaNames)
        {
            Assert.False(
                IsInlineableWrapperName(name),
                $"Inlineable wrapper component '{name}' should have been removed from components.schemas.");
        }
    }

    [Fact]
    public async Task Property_NonEmptyEnumerable_With_MaxLength_Carries_Min_And_MaxItems()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-tags"));
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
    private IEnumerable<JsonElement> WalkSchemaLayers(JsonElement doc, JsonElement schema)
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

    protected int? CollectMaxInt(JsonElement doc, JsonElement schema, string keyword)
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

    protected int? CollectMinInt(JsonElement doc, JsonElement schema, string keyword)
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

    protected string? CollectFirstString(JsonElement doc, JsonElement schema, string keyword)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (layer.TryGetProperty(keyword, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }

    private decimal? CollectMaxLowerBound(JsonElement doc, JsonElement schema)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (TryReadInclusiveLower(layer, out var v) && (best is null || v > best)) best = v;
        }
        return best;
    }

    protected decimal? CollectMinUpperBound(JsonElement doc, JsonElement schema)
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

    protected void AssertExclusiveLowerBoundReachable(JsonElement doc, JsonElement schema, decimal expected)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema))
        {
            if (!layer.TryGetProperty("exclusiveMinimum", out var ex)) continue;

            if (Version == OpenApiVersion.V3_1)
            {
                if (ex.ValueKind == JsonValueKind.Number && ex.GetDecimal() == expected) return;
            }
            else
            {
                if (ex.ValueKind == JsonValueKind.True
                    && layer.TryGetProperty("minimum", out var min)
                    && min.ValueKind == JsonValueKind.Number
                    && min.GetDecimal() == expected) return;
            }
        }

        Assert.Fail($"Expected {Version} exclusive lower bound of {expected} reachable from this schema, but none found.");
    }
}
