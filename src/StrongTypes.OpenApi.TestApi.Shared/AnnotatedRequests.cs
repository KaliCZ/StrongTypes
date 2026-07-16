using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.OpenApi.TestApi.Shared;

// Each annotated wrapper property is paired with a primitive `*Raw` twin so the tests can pin
// the same wire keywords on both surfaces.
public sealed record AnnotatedTextsRequest(
    [property: StringLength(50, MinimumLength = 3)]
    [property: RegularExpression("^[a-zA-Z0-9_]+$")]
    NonEmptyString Username,

    [property: StringLength(50, MinimumLength = 3)]
    [property: RegularExpression("^[a-zA-Z0-9_]+$")]
    string UsernameRaw,

    [property: StringLength(254)]
    [property: RegularExpression(@"^[^@]+@[^@]+$")]
    NonEmptyString Email,

    [property: StringLength(254)]
    [property: RegularExpression(@"^[^@]+@[^@]+$")]
    string EmailRaw,

    [property: EmailAddress]
    NonEmptyString ContactEmail,

    [property: EmailAddress]
    string ContactEmailRaw,

    [property: Url]
    NonEmptyString WebsiteUrl,

    [property: Url]
    string WebsiteUrlRaw,

    [property: Length(2, 8)]
    NonEmptyString Slug,

    [property: Length(2, 8)]
    string SlugRaw,

    [property: Base64String]
    NonEmptyString EncodedBlob,

    [property: Base64String]
    string EncodedBlobRaw,

    [property: System.ComponentModel.Description("Short user tagline")]
    NonEmptyString Tagline,

    [property: System.ComponentModel.Description("Short user tagline")]
    string TaglineRaw,

    NonEmptyString Description,

    [property: StringLength(100)]
    Email AnnotatedEmail,

    [property: StringLength(100)]
    string AnnotatedEmailRaw);

public sealed record AnnotatedNumbersRequest(
    [property: Range(18, 120)] Positive<int> Age,
    [property: Range(18, 120)] int AgeRaw,
    [property: Range(2, 8)] Digit Digit,
    [property: Range(2, 8)] int DigitRaw,
    [property: Range(-5, 5)] Positive<int> RangeAcrossFloor,
    [property: Range(2, 10, MinimumIsExclusive = true)] Positive<int> ExclusiveLowerAge,
    [property: Range(2, 10, MinimumIsExclusive = true)] int ExclusiveLowerAgeRaw,
    [property: Range(0, 5, MinimumIsExclusive = true)] Positive<int> ExclusiveAtFloor,
    [property: Range(1, 5)] Positive<int> InclusiveJustAboveFloor,
    [property: Range(-10, -5)] Positive<int> RangeBelowFloor);

public sealed record AnnotatedTagsRequest(
    [property: MaxLength(10)] NonEmptyEnumerable<NonEmptyString> Tags,
    [property: MaxLength(10)] string[] TagsRaw);

// Pairs each required-ness variant with a primitive `*Raw` twin; tests assert the two have
// identical `required` membership.
#pragma warning disable CS8618 // Non-nullable property is uninitialized — STJ assigns it.
public sealed record RequiredVariantsRequest
{
    public NonEmptyString Plain { get; init; }
    public string PlainRaw { get; init; }

    public NonEmptyString? Nullable { get; init; }
    public string? NullableRaw { get; init; }

    [Required] public NonEmptyString WithAttribute { get; init; }
    [Required] public string WithAttributeRaw { get; init; }

    [Required] public NonEmptyString? AttributeNullable { get; init; }
    [Required] public string? AttributeNullableRaw { get; init; }

    public required NonEmptyString WithKeyword { get; init; }
    public required string WithKeywordRaw { get; init; }

    public required NonEmptyString? KeywordNullable { get; init; }
    public required string? KeywordNullableRaw { get; init; }
}
#pragma warning restore CS8618
