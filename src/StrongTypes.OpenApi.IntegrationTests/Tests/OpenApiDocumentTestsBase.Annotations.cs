using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

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
//
// Each wrapper-typed test below has a primitive-typed sibling that
// pins the same wire keywords on a plain `string` / `int` / `string[]`
// carrying the same annotations. The sibling serves as a baseline:
// each pipeline's underlying annotation handling must natively produce
// these keywords on a primitive-typed property for the matching
// wrapper-typed assertion to be meaningful. If a primitive-typed test
// fails, the failure is in the pipeline's own annotation-mapping step,
// not in our wrapper paint.
public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Property_NonEmptyString_With_StringLength_And_Pattern_Carries_All_Three()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var username = Property(body, "username");

        Assert.Equal(3, CollectMaxInt(doc, username, "minLength", Version));
        Assert.Equal(50, CollectMinInt(doc, username, "maxLength", Version));
        Assert.Equal("^[a-zA-Z0-9_]+$", CollectFirstString(doc, username, "pattern", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_StringLength_Floors_To_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var email = Property(body, "email");

        // [StringLength(254)] sets only an upper bound; the wrapper's
        // floor of 1 must remain.
        Assert.Equal(1, CollectMaxInt(doc, email, "minLength", Version));
        Assert.Equal(254, CollectMinInt(doc, email, "maxLength", Version));
        Assert.Equal("^[^@]+@[^@]+$", CollectFirstString(doc, email, "pattern", Version));
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
        Assert.Equal(1, CollectMaxInt(doc, contactEmail, "minLength", Version));
        Assert.Equal(IsEmailStringFormatBroken ? null : "email", CollectFirstString(doc, contactEmail, "format", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_Without_Annotations_Renders_Plain_Wrapper()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var description = Property(body, "description");

        Assert.Equal(1, CollectMaxInt(doc, description, "minLength", Version));
        Assert.Null(CollectMinInt(doc, description, "maxLength", Version));
        Assert.Null(CollectFirstString(doc, description, "pattern", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Url_Carries_Format_Uri_And_Wrapper_MinLength()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var websiteUrl = Property(body, "websiteUrl");

        Assert.Equal(1, CollectMaxInt(doc, websiteUrl, "minLength", Version));
        Assert.Equal("uri", CollectFirstString(doc, websiteUrl, "format", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Length_Tightens_Both_Bounds_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var slug = Property(body, "slug");

        Assert.Equal(2, CollectMaxInt(doc, slug, "minLength", Version));
        Assert.Equal(8, CollectMinInt(doc, slug, "maxLength", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Base64String_Carries_Wrapper_MinLength_And_Format_Byte_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var encodedBlob = Property(body, "encodedBlob");

        Assert.Equal(1, CollectMaxInt(doc, encodedBlob, "minLength", Version));
        Assert.Equal("byte", CollectFirstString(doc, encodedBlob, "format", Version));
    }

    [Fact]
    public async Task Property_NonEmptyString_With_Description_Carries_Description_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var tagline = Property(body, "tagline");

        Assert.Equal("Short user tagline", CollectFirstString(doc, tagline, "description", Version));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Carries_Both_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var age = Property(body, "age");

        Assert.Equal(18m, CollectMaxLowerBound(doc, age, Version));
        Assert.Equal(120m, CollectMinUpperBound(doc, age, Version));
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
        Assert.Equal(10m, CollectMinUpperBound(doc, exclusive, Version));
        if (IsExclusiveRangeBroken)
            Assert.Equal(1m, CollectMaxLowerBound(doc, exclusive, Version));
        else
            AssertExclusiveLowerBoundReachable(doc, exclusive, 1m, Version);
    }

    [Fact]
    public async Task Property_NonEmptyEnumerable_With_MaxLength_Carries_Min_And_MaxItems()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-tags"));
        var tags = Property(body, "tags");

        Assert.Equal(1, CollectMaxInt(doc, tags, "minItems", Version));
        Assert.Equal(10, CollectMinInt(doc, tags, "maxItems", Version));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Across_Floor_Lets_Wrapper_Floor_Win()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var range = Property(body, "rangeAcrossFloor");

        // [Range(-5, 5)] would loosen the lower bound to -5, but the
        // wrapper's exclusiveMinimum:0 floor wins. Upper bound 5 stays.
        AssertExclusiveLowerBoundReachable(doc, range, 0m, Version);
        Assert.Equal(5m, CollectMinUpperBound(doc, range, Version));
    }

    [Fact]
    public async Task Property_String_With_StringLength_And_Pattern_Carries_All_Three()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var usernameRaw = Property(body, "usernameRaw");

        Assert.Equal(3, CollectMaxInt(doc, usernameRaw, "minLength", Version));
        Assert.Equal(50, CollectMinInt(doc, usernameRaw, "maxLength", Version));
        Assert.Equal("^[a-zA-Z0-9_]+$", CollectFirstString(doc, usernameRaw, "pattern", Version));
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
        Assert.Equal(0, CollectMaxInt(doc, emailRaw, "minLength", Version));
        Assert.Equal(254, CollectMinInt(doc, emailRaw, "maxLength", Version));
        Assert.Equal("^[^@]+@[^@]+$", CollectFirstString(doc, emailRaw, "pattern", Version));
    }

    [Fact]
    public async Task Property_String_With_EmailAddress_Carries_Format_Email_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var contactEmailRaw = Property(body, "contactEmailRaw");

        Assert.Equal(IsEmailStringFormatBroken ? null : "email", CollectFirstString(doc, contactEmailRaw, "format", Version));
    }

    [Fact]
    public async Task Property_String_With_Url_Carries_Format_Uri()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var websiteUrlRaw = Property(body, "websiteUrlRaw");

        Assert.Equal("uri", CollectFirstString(doc, websiteUrlRaw, "format", Version));
    }

    [Fact]
    public async Task Property_String_With_Length_Tightens_Both_Bounds_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var slugRaw = Property(body, "slugRaw");

        Assert.Equal(2, CollectMaxInt(doc, slugRaw, "minLength", Version));
        Assert.Equal(8, CollectMinInt(doc, slugRaw, "maxLength", Version));
    }

    [Fact]
    public async Task Property_String_With_Base64String_Carries_Format_Byte_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var encodedBlobRaw = Property(body, "encodedBlobRaw");

        Assert.Equal("byte", CollectFirstString(doc, encodedBlobRaw, "format", Version));
    }

    [Fact]
    public async Task Property_String_With_Description_Carries_Description_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var taglineRaw = Property(body, "taglineRaw");

        Assert.Equal("Short user tagline", CollectFirstString(doc, taglineRaw, "description", Version));
    }

    [Fact]
    public async Task Property_Int_With_Range_Carries_Both_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var ageRaw = Property(body, "ageRaw");

        Assert.Equal(18m, CollectMaxLowerBound(doc, ageRaw, Version));
        Assert.Equal(120m, CollectMinUpperBound(doc, ageRaw, Version));
    }

    [Fact]
    public async Task Property_Int_With_Range_MinimumIsExclusive_Carries_Exclusive_Lower_Bound_When_Honored()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var exclusiveRaw = Property(body, "exclusiveLowerAgeRaw");

        Assert.Equal(10m, CollectMinUpperBound(doc, exclusiveRaw, Version));
        if (IsExclusiveRangeBroken)
            Assert.Equal(1m, CollectMaxLowerBound(doc, exclusiveRaw, Version));
        else
            AssertExclusiveLowerBoundReachable(doc, exclusiveRaw, 1m, Version);
    }

    [Fact]
    public async Task Property_StringArray_With_MaxLength_Carries_MaxItems()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-tags"));
        var tagsRaw = Property(body, "tagsRaw");

        Assert.Equal(10, CollectMinInt(doc, tagsRaw, "maxItems", Version));
    }
}
