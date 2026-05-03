using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Exercises every non-body model-binding source supported by ASP.NET Core MVC
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>, <c>[FromForm]</c>,
/// and implicit query/route binding) for the strong types that have a
/// <c>TryParse</c> path. Every endpoint echoes the bound values back as JSON
/// so integration tests can assert the round-trip.
/// </summary>
[ApiController]
[Route("binding-probe")]
public sealed class BindingProbeController : ControllerBase
{
    [HttpGet("query")]
    public IActionResult FromQuery(
        [FromQuery] NonEmptyString name,
        [FromQuery] Positive<int> count,
        [FromQuery] Email? email)
        => Ok(new { name = name.Value, count = count.Value, email = email?.Address });

    [HttpGet("query-implicit")]
    public IActionResult ImplicitQuery(NonEmptyString name, Positive<int> count)
        => Ok(new { name = name.Value, count = count.Value });

    [HttpGet("route/{name}/{count:int}")]
    public IActionResult FromRoute(
        [FromRoute] NonEmptyString name,
        [FromRoute] Positive<int> count)
        => Ok(new { name = name.Value, count = count.Value });

    [HttpGet("header")]
    public IActionResult FromHeader(
        [FromHeader(Name = "X-Name")] NonEmptyString name,
        [FromHeader(Name = "X-Count")] Positive<int> count)
        => Ok(new { name = name.Value, count = count.Value });

    [HttpPost("form")]
    public IActionResult FromForm([FromForm] BindingProbeFormRequest request)
        => Ok(new
        {
            name = request.Name.Value,
            count = request.Count.Value,
            email = request.Email?.Address,
        });
}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    Positive<int> Count,
    Email? Email);
