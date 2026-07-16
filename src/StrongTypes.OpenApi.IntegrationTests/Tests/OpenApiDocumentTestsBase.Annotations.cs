using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

// Each wrapper-typed test has a primitive-typed sibling pinning the same keywords on a plain string/int/array, so a
// failing sibling attributes the fault to the pipeline's own annotation mapping rather than the wrapper paint.
public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Property_Email_With_StringLength_Tightens_MaxLength_Below_Wrapper_Ceiling()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-texts"));
        var annotatedEmail = Property(body, "annotatedEmail");

        Assert.Equal(1, CollectMaxInt(doc, annotatedEmail, "minLength", Version));
        Assert.Equal(100, CollectMinInt(doc, annotatedEmail, "maxLength", Version));
        Assert.Equal("email", CollectFirstString(doc, annotatedEmail, "format", Version));
    }

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

        Assert.Equal(1, CollectMaxInt(doc, contactEmail, "minLength", Version));
        Assert.Equal(IsEmailAddressFormatIgnored ? null : "email", CollectFirstString(doc, contactEmail, "format", Version));
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
    public async Task Property_Positive_Int_With_Range_MinimumIsExclusive_Carries_Exclusive_Lower_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var exclusive = Property(body, "exclusiveLowerAge");

        Assert.Equal(10m, CollectMinUpperBound(doc, exclusive, Version));
        AssertExclusiveLowerBoundReachable(doc, exclusive, 2m, Version);
    }

    [Fact]
    public async Task Property_Digit_With_Range_Carries_Caller_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var digit = Property(body, "digit");

        Assert.Equal(2m, CollectMaxLowerBound(doc, digit, Version));
        Assert.Equal(8m, CollectMinUpperBound(doc, digit, Version));
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

        AssertExclusiveLowerBoundReachable(doc, range, 0m, Version);
        Assert.Equal(5m, CollectMinUpperBound(doc, range, Version));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Exclusive_Range_At_Wrapper_Floor_Stays_Exclusive_At_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var atFloor = Property(body, "exclusiveAtFloor");

        AssertExclusiveLowerBoundReachable(doc, atFloor, 0m, Version);
        Assert.Equal(5m, CollectMinUpperBound(doc, atFloor, Version));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Inclusive_Range_Just_Above_Wrapper_Floor_Tightens_To_Inclusive_One()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var aboveFloor = Property(body, "inclusiveJustAboveFloor");

        // >= 1 is strictly tighter than the wrapper's > 0: JSON Schema bounds are real-valued, so > 0 alone admits 0.5.
        Assert.Equal(1m, CollectMaxLowerBound(doc, aboveFloor, Version));
        Assert.Equal(5m, CollectMinUpperBound(doc, aboveFloor, Version));
    }

    [Fact]
    public async Task Property_Positive_Int_With_Range_Below_Wrapper_Floor_Keeps_Wrapper_Floor_And_Caller_Maximum()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var below = Property(body, "rangeBelowFloor");

        // The merged bounds are unsatisfiable (> 0 with maximum -5); the pipeline doesn't detect the contradiction.
        AssertExclusiveLowerBoundReachable(doc, below, 0m, Version);
        Assert.Equal(-5m, CollectMinUpperBound(doc, below, Version));
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

        // Both pipelines write [StringLength]'s implicit MinimumLength = 0 verbatim as minLength: 0.
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

        Assert.Equal(IsEmailAddressFormatIgnored ? null : "email", CollectFirstString(doc, contactEmailRaw, "format", Version));
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
    public async Task Property_Int_With_Range_MinimumIsExclusive_Carries_Exclusive_Lower_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var exclusiveRaw = Property(body, "exclusiveLowerAgeRaw");

        Assert.Equal(10m, CollectMinUpperBound(doc, exclusiveRaw, Version));
        AssertExclusiveLowerBoundReachable(doc, exclusiveRaw, 2m, Version);
    }

    [Fact]
    public async Task Property_Int_Digit_Baseline_With_Range_Carries_Caller_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/annotated-numbers"));
        var digitRaw = Property(body, "digitRaw");

        Assert.Equal(2m, CollectMaxLowerBound(doc, digitRaw, Version));
        Assert.Equal(8m, CollectMinUpperBound(doc, digitRaw, Version));
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
