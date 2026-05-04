using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.AspNetCore.TestApi.Controllers;

[ApiController]
[Route("binding-probe")]
public sealed class BindingProbeController : ControllerBase
{
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

    [HttpPost("form")]
    public IActionResult FromForm([FromForm] BindingProbeFormRequest request)
        => Ok(new
        {
            tags = request.Tags?.Select(t => t.Value).ToArray(),
            counts = request.Counts?.Select(c => c.Value).ToArray(),
            digits = request.Digits?.Select(d => (int)d.Value).ToArray(),
            displayNameState = MaybeState(request.DisplayName),
            displayName = MaybeValue(request.DisplayName),
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
    NonEmptyEnumerable<NonEmptyString>? Tags,
    NonEmptyEnumerable<Positive<int>>? Counts,
    NonEmptyEnumerable<Digit>? Digits,
    Maybe<NonEmptyString>? DisplayName);
