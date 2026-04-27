using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Echo endpoint for the OpenAPI integration tests. Exposes
/// <see cref="NestedStrongTypesRequest"/> so the generated schema can be
/// inspected for deeply-nested strong-type combinations.
/// </summary>
[ApiController]
[Route("nested-strong-types")]
public sealed class NestedStrongTypesController : ControllerBase
{
    [HttpPost]
    public ActionResult<NestedStrongTypesRequest> Echo(NestedStrongTypesRequest request) => Ok(request);
}
