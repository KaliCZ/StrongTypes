using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
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
    public async Task Nullable_NonEmptyString_Property_Still_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-empty-string-entities"));
        var nullableValue = UnwrapNullableProperty(Property(body, "nullableValue"), Version);

        AssertInlineSchema(nullableValue);
        Assert.Equal("string", StringOrNull(nullableValue, "type"));
        Assert.Equal(1, IntOrNull(nullableValue, "minLength"));
    }

    [Fact]
    public async Task Nullable_NonEmptyString_On_Dedicated_Nullables_Endpoint_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = UnwrapNullableProperty(Property(body, "nullableNonEmptyString"), Version);

        AssertInlineSchema(value);
        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
    }
}
