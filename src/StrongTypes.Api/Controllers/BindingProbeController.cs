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
    public IActionResult ImplicitQuery(
        NonEmptyString name,
        NonEmptyString? nullableName,
        Positive<int> count,
        Positive<int>? nullableCount)
        => Ok(new
        {
            name = name.Value,
            nullableName = nullableName?.Value,
            count = count.Value,
            nullableCount = (int?)nullableCount?.Value,
        });

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
            tags = request.Tags?.Select(t => t.Value).ToArray(),
            counts = request.Counts?.Select(c => c.Value).ToArray(),
            digits = request.Digits?.Select(d => (int)d.Value).ToArray(),
            displayNameState = MaybeState(request.DisplayName),
            displayName = MaybeValue(request.DisplayName),
        });

    [HttpGet("query-nee")]
    public IActionResult NonEmptyEnumerableFromQuery(
        [FromQuery] NonEmptyEnumerable<Positive<int>> counts,
        [FromQuery] NonEmptyEnumerable<NonEmptyString>? tags,
        [FromQuery] NonEmptyEnumerable<Digit>? digits)
        => Ok(new
        {
            counts = counts.Select(c => c.Value).ToArray(),
            tags = tags?.Select(t => t.Value).ToArray(),
            digits = digits?.Select(d => (int)d.Value).ToArray(),
        });

    [HttpGet("route-nee/{counts}")]
    public IActionResult NonEmptyEnumerableFromRoute([FromRoute] NonEmptyEnumerable<Positive<int>> counts)
        => Ok(new { counts = counts.Select(c => c.Value).ToArray() });

    [HttpGet("header-nee")]
    public IActionResult NonEmptyEnumerableFromHeader(
        [FromHeader(Name = "X-Counts")] NonEmptyEnumerable<Positive<int>> counts,
        [FromHeader(Name = "X-Tags")] NonEmptyEnumerable<NonEmptyString>? tags,
        [FromHeader(Name = "X-Digits")] NonEmptyEnumerable<Digit>? digits)
        => Ok(new
        {
            counts = counts.Select(c => c.Value).ToArray(),
            tags = tags?.Select(t => t.Value).ToArray(),
            digits = digits?.Select(d => (int)d.Value).ToArray(),
        });

    [HttpGet("query-nee-strong")]
    public IActionResult StrongTypedFromQuery(
        [FromQuery] NonEmptyEnumerable<NonEmptyString> tags,
        [FromQuery] NonEmptyEnumerable<Positive<int>> counts,
        [FromQuery] NonEmptyEnumerable<Digit> digits)
        => Ok(new
        {
            tags = tags.Select(t => t.Value).ToArray(),
            counts = counts.Select(c => c.Value).ToArray(),
            digits = digits.Select(d => (int)d.Value).ToArray(),
        });

    private static string MaybeState<T>(Maybe<T>? maybe)
        where T : notnull
        => maybe is null ? "missing" : maybe.Value.IsSome ? "some" : "none";

    private static string? MaybeValue(Maybe<NonEmptyString>? maybe)
        => maybe is null ? null : maybe.Value.Match<string?>(v => v.Value, () => null);

}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    NonEmptyString? NullableName,
    Positive<int> Count,
    Positive<int>? NullableCount,
    Email Email,
    Email? NullableEmail,
    NonEmptyEnumerable<NonEmptyString>? Tags,
    NonEmptyEnumerable<Positive<int>>? Counts,
    NonEmptyEnumerable<Digit>? Digits,
    Maybe<NonEmptyString>? DisplayName);
