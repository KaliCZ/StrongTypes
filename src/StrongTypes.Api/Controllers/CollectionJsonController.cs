#nullable enable

using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Collection endpoints for the JSON-contract integration-test suite. Each POST
/// returns the deserialized DTO verbatim — exercising both halves of the round
/// trip (STJ deserialization + ASP.NET Core validation on the way in, STJ
/// serialization on the way out). No persistence: collections aren't part of the
/// EF Core story yet, and these tests are purely about wire-format handling.
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
