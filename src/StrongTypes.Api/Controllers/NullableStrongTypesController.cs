using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Echo endpoint for the OpenAPI integration tests. Exposes
/// <see cref="NullableStrongTypesRequest"/> so the generated schema can be
/// inspected for nullable strong-type properties.
/// </summary>
[ApiController]
[Route("nullable-strong-types")]
public sealed class NullableStrongTypesController : ControllerBase
{
    [HttpPost]
    public ActionResult<NullableStrongTypesRequest> Echo(NullableStrongTypesRequest request) => Ok(request);
}
