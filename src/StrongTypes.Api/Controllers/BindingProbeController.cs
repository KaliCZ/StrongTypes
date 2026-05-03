using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Exercises every non-body model-binding source supported by ASP.NET Core MVC
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>, <c>[FromForm]</c>,
/// and implicit query/route binding) for the strong types that have a
/// <c>TryParse</c> path. Each endpoint accepts a required and a nullable
/// variant of every wrapped type so round-trip tests cover both branches.
/// <c>[FromRoute]</c> is required-only — route segments are required by HTTP
/// semantics; the optional / nullable equivalent is <c>[FromQuery]</c>.
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
        => Ok(new
        {
            name = name.Value,
            nullableName = nullableName?.Value,
            count = count.Value,
            nullableCount = (int?)nullableCount?.Value,
            digit = (int)digit.Value,
            nullableDigit = (int?)nullableDigit?.Value,
            email = email.Address,
            nullableEmail = nullableEmail?.Address,
        });

    [HttpGet("query-implicit")]
    public IActionResult ImplicitQuery(NonEmptyString name, Positive<int> count)
        => Ok(new { name = name.Value, count = count.Value });

    [HttpGet("route/{name}/{count:int}/{digit}")]
    public IActionResult FromRoute(
        [FromRoute] NonEmptyString name,
        [FromRoute] Positive<int> count,
        [FromRoute] Digit digit)
        => Ok(new { name = name.Value, count = count.Value, digit = (int)digit.Value });

    [HttpGet("header")]
    public IActionResult FromHeader(
        [FromHeader(Name = "X-Name")] NonEmptyString name,
        [FromHeader(Name = "X-Nullable-Name")] NonEmptyString? nullableName,
        [FromHeader(Name = "X-Count")] Positive<int> count,
        [FromHeader(Name = "X-Nullable-Count")] Positive<int>? nullableCount)
        => Ok(new
        {
            name = name.Value,
            nullableName = nullableName?.Value,
            count = count.Value,
            nullableCount = (int?)nullableCount?.Value,
        });

    [HttpPost("form")]
    public IActionResult FromForm([FromForm] BindingProbeFormRequest request)
        => Ok(new
        {
            name = request.Name.Value,
            nullableName = request.NullableName?.Value,
            count = request.Count.Value,
            nullableCount = (int?)request.NullableCount?.Value,
            email = request.Email.Address,
            nullableEmail = request.NullableEmail?.Address,
        });
}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    NonEmptyString? NullableName,
    Positive<int> Count,
    Positive<int>? NullableCount,
    Email Email,
    Email? NullableEmail);
