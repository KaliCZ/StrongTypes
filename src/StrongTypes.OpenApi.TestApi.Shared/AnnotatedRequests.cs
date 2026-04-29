using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.OpenApi.TestApi.Shared;

// Wire-level contract for the annotation-preservation tests. The generators
// translate these data-annotations into OpenAPI bounds; our filter must not
// wipe them when painting the strong-type wire shape.
public sealed record AnnotatedTextsRequest(
    [property: StringLength(50, MinimumLength = 3)]
    [property: RegularExpression("^[a-zA-Z0-9_]+$")]
    NonEmptyString Username,

    [property: StringLength(254)]
    [property: RegularExpression(@"^[^@]+@[^@]+$")]
    NonEmptyString Email,

    [property: EmailAddress]
    NonEmptyString ContactEmail,

    [property: Url]
    NonEmptyString WebsiteUrl,

    [property: Length(2, 8)]
    NonEmptyString Slug,

    [property: Base64String]
    NonEmptyString EncodedBlob,

    [property: System.ComponentModel.Description("Short user tagline")]
    NonEmptyString Tagline,

    NonEmptyString Description);

public sealed record AnnotatedNumbersRequest(
    [property: Range(18, 120)] Positive<int> Age,
    [property: Range(-5, 5)] Positive<int> RangeAcrossFloor,
    [property: Range(1, 10, MinimumIsExclusive = true)] Positive<int> ExclusiveLowerAge);

public sealed record AnnotatedTagsRequest(
    [property: MaxLength(10)] NonEmptyEnumerable<NonEmptyString> Tags);
