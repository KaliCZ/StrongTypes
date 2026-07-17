using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.AspNetCore.TestApi.Controllers;

/// <summary>
/// Endpoints used to observe DataAnnotations validation applied to strong-typed
/// properties: the attribute must evaluate the wrapped value (e.g.
/// <see cref="RangeAttribute"/> reads it through <see cref="IConvertible"/>),
/// composing with the invariant the type already enforces at deserialization.
/// </summary>
[ApiController]
[Route("validation-probe")]
public sealed class ValidationProbeController : ControllerBase
{
    [HttpPost("range")]
    public IActionResult Range(RangeBody body) => Ok();
}

public sealed record RangeBody(
    [Required][Range(1, 100)] Positive<int>? Quantity,
    [Range(0.5, 2.5)] Positive<decimal>? Factor);
