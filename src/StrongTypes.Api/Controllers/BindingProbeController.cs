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
        [FromHeader(Name = "X-Nullable-Count")] Positive<int>? nullableCount,
        [FromHeader(Name = "X-Digit")] Digit digit,
        [FromHeader(Name = "X-Nullable-Digit")] Digit? nullableDigit)
        => Ok(new
        {
            name = name.Value,
            nullableName = nullableName?.Value,
            count = count.Value,
            nullableCount = (int?)nullableCount?.Value,
            digit = (int)digit.Value,
            nullableDigit = (int?)nullableDigit?.Value,
        });

    [HttpPost("form")]
    public IActionResult FromForm([FromForm] BindingProbeFormRequest request)
        => Ok(new
        {
            name = request.Name.Value,
            nullableName = request.NullableName?.Value,
            count = request.Count.Value,
            nullableCount = (int?)request.NullableCount?.Value,
            debt = request.Debt.Value,
            nullableDebt = (decimal?)request.NullableDebt?.Value,
            digit = (int)request.Digit.Value,
            nullableDigit = (int?)request.NullableDigit?.Value,
            email = request.Email.Address,
            nullableEmail = request.NullableEmail?.Address,
        });

    [HttpGet("query-unsupported-nee")]
    public IActionResult UnsupportedNonEmptyEnumerableFromQuery(
        [FromQuery] NonEmptyEnumerable<NonEmptyString>? tags)
        => Ok(new { tagsBound = tags is not null });

    [HttpGet("query-unsupported-maybe")]
    public IActionResult UnsupportedMaybeFromQuery(
        [FromQuery] Maybe<NonEmptyString> displayName)
        => Ok(new { displayNameState = displayName.ToString() });

    [HttpGet("header-unsupported-nee")]
    public IActionResult UnsupportedNonEmptyEnumerableFromHeader(
        [FromHeader(Name = "X-Tags")] NonEmptyEnumerable<NonEmptyString>? tags)
        => Ok(new { tagsBound = tags is not null });

    [HttpGet("header-unsupported-maybe")]
    public IActionResult UnsupportedMaybeFromHeader(
        [FromHeader(Name = "X-Display-Name")] Maybe<NonEmptyString> displayName)
        => Ok(new { displayNameState = displayName.ToString() });

    [HttpPost("form-unsupported-nee")]
    public IActionResult UnsupportedNonEmptyEnumerableFromForm([FromForm] UnsupportedNonEmptyEnumerableFormRequest request)
        => Ok(new { tagsBound = request.Tags is not null });

    [HttpPost("form-unsupported-maybe")]
    public IActionResult UnsupportedMaybeFromForm([FromForm] UnsupportedMaybeFormRequest request)
        => Ok(new { displayNameState = request.DisplayName.ToString() });
}

public sealed record BindingProbeFormRequest(
    NonEmptyString Name,
    NonEmptyString? NullableName,
    Positive<int> Count,
    Positive<int>? NullableCount,
    Negative<decimal> Debt,
    Negative<decimal>? NullableDebt,
    Digit Digit,
    Digit? NullableDigit,
    Email Email,
    Email? NullableEmail);

public sealed record UnsupportedNonEmptyEnumerableFormRequest(
    NonEmptyEnumerable<NonEmptyString>? Tags);

public sealed record UnsupportedMaybeFormRequest(
    Maybe<NonEmptyString> DisplayName);
