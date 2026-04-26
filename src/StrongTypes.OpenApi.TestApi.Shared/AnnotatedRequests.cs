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

    NonEmptyString Description,

    NonEmptyString? OptionalNickname);

public sealed record AnnotatedNumbersRequest(
    [property: Range(18, 120)] Positive<int> Age,
    [property: Range(-5, 5)] Positive<int> RangeAcrossFloor);

public sealed record AnnotatedTagsRequest(
    [property: MaxLength(10)] NonEmptyEnumerable<NonEmptyString> Tags);

[ApiController]
[Route("annotated-texts")]
public sealed class AnnotatedTextsController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedTextsRequest request) => Ok(request);
}

[ApiController]
[Route("annotated-numbers")]
public sealed class AnnotatedNumbersController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedNumbersRequest request) => Ok(request);
}

[ApiController]
[Route("annotated-tags")]
public sealed class AnnotatedTagsController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedTagsRequest request) => Ok(request);
}
