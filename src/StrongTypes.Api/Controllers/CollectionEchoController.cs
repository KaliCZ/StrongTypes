#nullable enable

using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Echo endpoints for the collection integration-test suite. Each POST returns
/// the deserialized DTO verbatim — this exercises both the deserialize side
/// (STJ converters + ASP.NET Core validation) and the serialize side (JSON output
/// shape) in a single round trip. No persistence: collections aren't part of the
/// EF Core story yet, and these tests are purely about wire-format handling.
/// </summary>
[ApiController]
[Route("collections")]
public sealed class CollectionEchoController : ControllerBase
{
    [HttpPost("int")]
    public ActionResult<IntCollectionsRequest> EchoInt(IntCollectionsRequest request) => Ok(request);

    [HttpPost("positive-int")]
    public ActionResult<PositiveIntCollectionsRequest> EchoPositiveInt(PositiveIntCollectionsRequest request) => Ok(request);

    [HttpPost("string")]
    public ActionResult<StringCollectionsRequest> EchoString(StringCollectionsRequest request) => Ok(request);

    [HttpPost("non-empty-string")]
    public ActionResult<NonEmptyStringCollectionsRequest> EchoNonEmptyString(NonEmptyStringCollectionsRequest request) => Ok(request);
}
