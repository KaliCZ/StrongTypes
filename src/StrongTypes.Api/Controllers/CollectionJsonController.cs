using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Each POST echoes the deserialized DTO back verbatim, exercising both halves of the JSON
/// round trip. No persistence — collections are not part of the EF Core story yet.
/// </summary>
[ApiController]
[Route("collections")]
public sealed class CollectionJsonController : ControllerBase
{
    [HttpPost("int")]
    public ActionResult<IntCollectionsRequest> Int(IntCollectionsRequest request) => Ok(request);

    [HttpPost("positive-int")]
    public ActionResult<PositiveIntCollectionsRequest> PositiveInt(PositiveIntCollectionsRequest request) => Ok(request);

    [HttpPost("string")]
    public ActionResult<StringCollectionsRequest> String(StringCollectionsRequest request) => Ok(request);

    [HttpPost("non-empty-string")]
    public ActionResult<NonEmptyStringCollectionsRequest> NonEmptyString(NonEmptyStringCollectionsRequest request) => Ok(request);
}
