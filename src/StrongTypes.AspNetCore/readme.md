# Kalicz.StrongTypes.AspNetCore

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.AspNetCore?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.AspNetCore?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

A small companion package to
[Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
You only need it if you want to bind `NonEmptyEnumerable<T>` from
`[FromForm]`, `[FromQuery]`, `[FromHeader]`, or `[FromRoute]`.

If you're writing a standard JSON API, you don't need this package.
`[FromBody]` round-tripping for `NonEmptyEnumerable<T>` — and
arbitrary nesting with the other wrappers — already works through
the JSON converters that ship with the main `Kalicz.StrongTypes`
package.

The other wrappers (`NonEmptyString`, `Email`, `Digit`, the numeric
ones) bind from every non-body source out of the box because they
implement `IParsable<TSelf>` and ASP.NET Core's built-in `TryParse`
binder picks them up — no extra package needed for those either.

`Maybe<T>` is intentionally **not** supported on non-body slots. On
the wire, a query / route / header value is either present or
absent — there is no protocol-level "explicitly null" — so the
three-state semantic that motivates `Maybe<T>?` collapses to
two-state, which `T?` already covers. Forms have the same problem
(`application/x-www-form-urlencoded` carries strings, not nulls), so
they're excluded for the same reason. Use `Maybe<T>` from
`[FromBody]` if you need three-state PATCH semantics — the JSON
converter handles that natively.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.AspNetCore
```

## Register

One call on the service collection:

```csharp
builder.Services.AddControllers();
builder.Services.AddStrongTypes();
```

`AddStrongTypes()` inserts the `NonEmptyEnumerable<T>` binder
provider at the front of `MvcOptions.ModelBinderProviders`, so it
runs before the default collection binders.

## Example

A multi-value form field (or query string) that must be non-empty:

```csharp
[HttpPost("articles")]
public IActionResult Create([FromForm] NonEmptyEnumerable<NonEmptyString> tags)
{
    // tags is guaranteed non-empty; the framework returns 400 + problem
    // details if the caller submitted zero tag fields, or any tag is
    // empty / whitespace-only.
    return Ok(new { count = tags.Count });
}
```

## Supported element types

The binder parses each raw string via `IParsable<T>` on the element
type. That covers:

- Every BCL primitive and value type that implements `IParsable<T>`
  (`int`, `long`, `Guid`, `DateTime`, `decimal`, …) — every numeric
  type in net7+ does.
- Every Kalicz.StrongTypes wrapper — `NonEmptyString`, `Email`,
  `Digit`, `Positive<T>`, `Negative<T>`, `NonNegative<T>`,
  `NonPositive<T>`. They all implement `IParsable<TSelf>` and round-trip
  through their `TryCreate` factory, so wire-level invariant violations
  (empty `NonEmptyString`, non-positive `Positive<int>`, malformed
  `Email`) surface as 400 + `ValidationProblemDetails`.

Wrapper-of-wrapper combinations
(`NonEmptyEnumerable<NonEmptyEnumerable<T>>`) are genuinely out of
scope: they have no clean wire form on a non-body source. Use
`[FromBody]` for those — the JSON converters in the main package
handle arbitrary nesting.

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
