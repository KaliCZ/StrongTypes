# Kalicz.StrongTypes.AspNetCore

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.AspNetCore?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.AspNetCore?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

A small companion package to
[Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
It does two things:

1. Binds `NonEmptyEnumerable<T>` from `[FromForm]`, `[FromQuery]`,
   `[FromHeader]`, or `[FromRoute]`.
2. Normalizes JSON request-body validation error keys so a failed
   strong type is reported under its property name (`Value`) rather
   than the System.Text.Json path (`$.value`) — see
   [JSON error-key normalization](#json-error-key-normalization).

For *binding*, a standard JSON API doesn't need this package.
`[FromBody]` round-tripping for `NonEmptyEnumerable<T>` — and
arbitrary nesting with the other wrappers — already works through
the JSON converters that ship with the main `Kalicz.StrongTypes`
package. The error-key normalization is the reason a JSON API might
still want it.

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
runs before the default collection binders, and enables JSON
error-key normalization. Configure via the overload:

```csharp
builder.Services.AddStrongTypes(o =>
{
    o.NormalizeJsonErrorKeys = true;                      // default; false to opt out
    o.JsonErrorKeyCasing = JsonErrorKeyCasing.PascalCase; // default
});
```

## JSON error-key normalization

When a strong type fails to deserialize from a JSON request body, the
System.Text.Json input formatter keys the `ValidationProblemDetails`
error by the **JSON path** (`$.value`), whereas data-annotation and
model-binding errors key by the **property name** (`Value`).
`AddStrongTypes()` rewrites the `$.`-prefixed body keys to the
property-name form so the API reports one key convention.

- **Opt-out, on by default.** Set `NormalizeJsonErrorKeys = false` to
  keep the raw `$.value` paths.
- **Scope.** Affects only the automatic `[ApiController]`
  `ValidationProblemDetails` response — not System.Text.Json itself,
  raw `JsonSerializer` calls, or minimal-API binding. Model-binding
  errors (no `$.` prefix) pass through untouched. It rewrites every
  JSON-body error key, not only strong-type ones.
- **Casing** (`JsonErrorKeyCasing`): `PascalCase` (default, matching
  the C# property name data annotations use by default), `CamelCase`,
  or `StripOnly` (drop the `$.` prefix, keep the wire casing). A custom
  `[JsonPropertyName]` that isn't just a re-cased property name can't
  be recovered from the path.

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
