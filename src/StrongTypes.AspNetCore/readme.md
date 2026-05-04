# Kalicz.StrongTypes.AspNetCore

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.AspNetCore?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.AspNetCore?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

A small, niche companion package to
[Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
Most apps will not need it. It only matters if you're posting HTML
forms (or, occasionally, accepting list query strings) and you want
two specific wrappers — `Maybe<T>` and `NonEmptyEnumerable<T>` — to
bind from those non-body sources.

If you're shipping a JSON API with `[FromBody]`, stop reading: the
JSON converters in `Kalicz.StrongTypes` already handle every wrapper,
including these two, with arbitrary nesting. You don't need this
package.

## When you actually want this package

The intended scenario is **server-rendered HTML forms** posting back
to MVC controllers:

- **`Maybe<T>` for patch contracts.** A form field that the user can
  leave alone, clear, or set needs three states. `Maybe<T>?` gives you
  exactly that: `null` = field omitted (don't touch it), `None` =
  field present but empty (clear it), `Some(value)` = set it. This is
  the main reason the package exists.
- **`NonEmptyEnumerable<T>` for repeated form fields** — multi-select
  inputs, checkbox groups, dynamic row collections — where you want
  the type system to express "at least one was submitted." The same
  binder also works for repeated query parameters
  (`?tags=a&tags=b`), so list-style search filters are a reasonable
  secondary use.

Everything else — `NonEmptyString`, `Email`, `Digit`, the numeric
wrappers — already binds from `[FromQuery]`, `[FromRoute]`,
`[FromHeader]`, and `[FromForm]` without this package, because they
implement `IParsable<TSelf>` and ASP.NET Core's built-in `TryParse`
binder picks them up.

## What's not worth using

- **`Maybe<T>` from query strings or headers.** The binder *will*
  bind it (omitted key → `None` on a non-nullable target or `null`
  on `Maybe<T>?`, present key → `Some(parse(s))`), but the
  three-state distinction that makes `Maybe<T>` valuable in form
  patches doesn't really survive on a query string or a header —
  there's no idiomatic way for a caller to spell "present but
  empty" vs "omitted," so callers can't actually drive the third
  state. Use plain `T?` there.
- **`[FromBody]`.** Already covered by the JSON converters in the
  main package — no need for this package at all.

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

`AddStrongTypes()` inserts both binder providers at the front of
`MvcOptions.ModelBinderProviders`, so they run before the default
collection / simple-type binders.

## Examples

The primary use — a form-posted patch contract:

```csharp
[HttpPost("profile")]
public IActionResult PatchProfile([FromForm] ProfilePatch patch)
{
    // DisplayName == null: field omitted, leave it alone.
    // DisplayName == None: field was present but empty, clear it.
    // DisplayName == Some(value): set it to a non-empty string.
    return Ok();
}

public sealed record ProfilePatch(Maybe<NonEmptyString>? DisplayName);
```

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

Both binders parse each raw string via `IParsable<T>` on the element
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
(`NonEmptyEnumerable<Maybe<T>>`, `Maybe<NonEmptyEnumerable<T>>`,
`NonEmptyEnumerable<NonEmptyEnumerable<T>>`) are genuinely out of
scope: they have no clean wire form on a non-body source. Use
`[FromBody]` for those — the JSON converters in the main package
handle arbitrary nesting.

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
