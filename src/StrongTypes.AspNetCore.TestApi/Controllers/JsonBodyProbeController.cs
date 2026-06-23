using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.AspNetCore.TestApi.Controllers;

/// <summary>
/// JSON-body endpoints used to observe the <c>ValidationProblemDetails</c> error
/// keys strong types produce through the System.Text.Json input formatter, with
/// and without <see cref="StrongTypesAspNetCoreOptions.NormalizeJsonErrorKeys"/>.
/// The request shapes cover a reference strong type and a struct strong type so
/// the divergent null-handling (post-binding required vs. parse-time failure) is
/// exercised on both.
/// </summary>
[ApiController]
[Route("json-body-probe")]
public sealed class JsonBodyProbeController : ControllerBase
{
    [HttpPost("non-empty-string")]
    public IActionResult NonEmptyString(NonEmptyStringBody body) => Ok();

    [HttpPost("positive-int")]
    public IActionResult PositiveInt(PositiveIntBody body) => Ok();

    [HttpPost("data-annotations")]
    public IActionResult DataAnnotations(DataAnnotationBody body) => Ok();
}

public sealed record DataAnnotationBody(
    [Required] string? Value,
    [EmailAddress] string? Email);

public sealed record NonEmptyStringBody(NonEmptyString Value, NonEmptyString? NullableValue);

public sealed record PositiveIntBody(Positive<int> Value, Positive<int>? NullableValue);
