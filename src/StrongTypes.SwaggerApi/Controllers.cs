using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.SwaggerApi;

[ApiController]
[Route("non-empty-string-entities")]
public sealed class NonEmptyStringEntityController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(ReferenceEntityRequest<NonEmptyString> request)
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
