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
[Route("non-positive-decimal-entities")]
public sealed class NonPositiveDecimalEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(StructEntityRequest<NonPositive<decimal>> request)
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
/// schema tests cover both branches. <c>Digit</c> is deliberately omitted
/// because it has no OpenAPI schema transformer yet; <c>[FromRoute]</c> is
/// required-only because route segments are required by HTTP semantics.
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
        [FromQuery] Email email,
        [FromQuery] Email? nullableEmail)
        => Ok();

    [HttpGet("route/{name}/{count:int}")]
    public IActionResult FromRoute(
        [FromRoute] NonEmptyString name,
        [FromRoute] Positive<int> count)
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
}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    NonEmptyString? NullableName,
    Positive<int> Count,
    Positive<int>? NullableCount,
    Email Email,
    Email? NullableEmail);
