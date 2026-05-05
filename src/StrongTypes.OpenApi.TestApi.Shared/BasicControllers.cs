using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.OpenApi.TestApi.Shared;

[ApiController]
[Route("non-empty-string-entities")]
public sealed class NonEmptyStringEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(ReferenceEntityRequest<NonEmptyString> request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("email-entities")]
public sealed class EmailEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(ReferenceEntityRequest<Email> request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("positive-int-entities")]
public sealed class PositiveIntEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(StructEntityRequest<Positive<int>> request)
        => Ok(new EntityResponse(Guid.NewGuid()));

    [HttpPatch("{id:guid}")]
    public IActionResult Patch(Guid id, StructEntityPatchRequest<Positive<int>> request)
        => Ok(new EntityResponse(id));
}

[ApiController]
[Route("non-negative-long-entities")]
public sealed class NonNegativeLongEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(StructEntityRequest<NonNegative<long>> request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("negative-double-entities")]
public sealed class NegativeDoubleEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(StructEntityRequest<Negative<double>> request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("digit-entities")]
public sealed class DigitEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(StructEntityRequest<Digit> request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("non-positive-decimal-entities")]
public sealed class NonPositiveDecimalEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(DecimalEntityRequest request)
        => Ok(new EntityResponse(Guid.NewGuid()));
}

[ApiController]
[Route("collections")]
public sealed class CollectionsController : ControllerBase
{
    [HttpPost("int")]
    public IActionResult Int(IntCollectionsRequest request) => Ok();

    [HttpPost("positive-int")]
    public IActionResult PositiveInt(PositiveIntCollectionsRequest request) => Ok();

    [HttpPost("non-empty-string")]
    public IActionResult NonEmptyString(NonEmptyStringCollectionsRequest request) => Ok();

    [HttpPost("shapes")]
    public IActionResult Shapes(CollectionShapesRequest request) => Ok();

    [HttpPost("dictionary-shapes")]
    public IActionResult DictionaryShapes(DictionaryShapesRequest request) => Ok();
}

[ApiController]
[Route("nullable-strong-types")]
public sealed class NullableStrongTypesController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(NullableStrongTypesRequest request) => Ok(request);
}

[ApiController]
[Route("nested-strong-types")]
public sealed class NestedStrongTypesController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(NestedStrongTypesRequest request) => Ok(request);
}

/// <summary>
/// Exercises non-body model-binding sources for strong-type parameters, so the
/// generated OpenAPI document can be asserted against. Mirrors the shape of
/// <c>StrongTypes.Api.Controllers.BindingProbeController</c>: each endpoint
/// accepts a required and a nullable variant of every wrapped type so the
/// schema tests cover both branches. <c>[FromRoute]</c> is required-only
/// because route segments are required by HTTP semantics.
/// </summary>
[ApiController]
[Route("binding-probe")]
public sealed class BindingProbeController : ControllerBase
{
    [HttpGet("query")]
    public IActionResult FromQuery(
        [FromQuery] NonEmptyString name,
        [FromQuery] NonEmptyString? nullableName,
        [FromQuery] Positive<int> count,
        [FromQuery] Positive<int>? nullableCount,
        [FromQuery] Digit digit,
        [FromQuery] Digit? nullableDigit,
        [FromQuery] Email email,
        [FromQuery] Email? nullableEmail)
        => Ok();

    [HttpGet("route/{name}/{count:int}/{digit}")]
    public IActionResult FromRoute(
        [FromRoute] NonEmptyString name,
        [FromRoute] Positive<int> count,
        [FromRoute] Digit digit)
        => Ok();

    [HttpGet("header")]
    public IActionResult FromHeader(
        [FromHeader(Name = "X-Name")] NonEmptyString name,
        [FromHeader(Name = "X-Nullable-Name")] NonEmptyString? nullableName,
        [FromHeader(Name = "X-Count")] Positive<int> count,
        [FromHeader(Name = "X-Nullable-Count")] Positive<int>? nullableCount)
        => Ok();

    [HttpPost("form")]
    public IActionResult FromForm([FromForm] BindingProbeFormRequest request) => Ok();

    // Annotated variants — pin that caller annotations attached at a non-body
    // strong-type slot (parameter or form property) reach the wire schema.

    // Each annotated wrapper-typed parameter has a primitive-typed sibling
    // carrying the same annotation. The sibling is the baseline: a pipeline
    // that doesn't surface the annotation on the primitive obviously can't
    // surface it on the wrapper either, so the wrapper tests only have
    // teeth when the primitive sibling carries the keyword.
    [HttpGet("query-annotated")]
    public IActionResult FromQueryAnnotated(
        [FromQuery, StringLength(50)] NonEmptyString name,
        [FromQuery, StringLength(50)] string plainName,
        [FromQuery, Range(5, 100)] Positive<int> count,
        [FromQuery, Range(5, 100)] int plainCount)
        => Ok();

    // Form bodies are split into two uniform requests — one all wrappers,
    // one all primitives — to keep the per-pipeline form-body shape
    // consistent. The `NonBodyStrongTypeOperationFilter` reshapes
    // Swashbuckle's `{ allOf: [<each>] }` form-body schema (emitted
    // whenever every field is component-typed) back into a proper
    // `{ properties: { … } }` map, so consumers see field names on both
    // pipelines.
    [HttpPost("form-annotated")]
    public IActionResult FromFormAnnotated([FromForm] AnnotatedBindingProbeFormRequest request) => Ok();

    [HttpPost("form-annotated-plain")]
    public IActionResult FromFormAnnotatedPlain([FromForm] PlainAnnotatedBindingProbeFormRequest request) => Ok();

    // The three endpoints below cover the form-body reshape on diverse
    // payload shapes: a primitives-only form (Swashbuckle emits a proper
    // properties map natively), an all-wrappers form with a mix of
    // annotations on multiple wrappers of the same type, and a mixed form
    // exercising both kinds together with caller annotations on each.

    [HttpPost("form-simple-types")]
    public IActionResult FromFormSimpleTypes([FromForm] SimpleTypesFormRequest request) => Ok();

    [HttpPost("form-all-wrappers")]
    public IActionResult FromFormAllWrappers([FromForm] AllStrongTypesFormRequest request) => Ok();

    [HttpPost("form-mixed")]
    public IActionResult FromFormMixed([FromForm] MixedFormRequest request) => Ok();
}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    NonEmptyString? NullableName,
    Positive<int> Count,
    Positive<int>? NullableCount,
    Digit Digit,
    NonEmptyEnumerable<NonEmptyString> Tags,
    Email Email,
    Email? NullableEmail);

public sealed record AnnotatedBindingProbeFormRequest(
    [property: StringLength(50)] NonEmptyString Name,
    [property: Range(5, 100)] Positive<int> Count);

public sealed record PlainAnnotatedBindingProbeFormRequest(
    [property: StringLength(50)] string PlainName,
    [property: Range(5, 100)] int PlainCount);

public sealed record SimpleTypesFormRequest(
    [property: StringLength(50)] string Title,
    [property: Range(0, 150)] int Age,
    bool IsActive,
    [property: StringLength(200)] string Description);

public sealed record AllStrongTypesFormRequest(
    [property: RegularExpression(@"^[A-Z]{3}-\d{4}$")] NonEmptyString Code,
    [property: Url] NonEmptyString Website,
    [property: StringLength(200)] NonEmptyString Description,
    [property: Range(1, 100)] Positive<int> Quantity,
    Email Contact,
    [property: MinLength(2)] NonEmptyEnumerable<Negative<decimal>> Losses);

public sealed record MixedFormRequest(
    [property: StringLength(100)] string Title,
    [property: RegularExpression(@"^[A-Z]{3}$")] NonEmptyString Code,
    [property: Range(1, 1000)] int Quantity,
    [property: Range(1, 100)] Positive<int> Stock,
    Email ContactEmail,
    NonEmptyEnumerable<NonEmptyString> Tags);
