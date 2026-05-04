# Kalicz.StrongTypes.AspNetCore

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.AspNetCore?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.AspNetCore?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.AspNetCore/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

ASP.NET Core MVC model binders for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes)
wrappers that don't fit the `IParsable<T>` single-string shape.

Most strong types (`NonEmptyString`, `Email`, `Digit`, the four numeric
wrappers) bind from `[FromQuery]`, `[FromRoute]`, `[FromHeader]`, and
`[FromForm]` automatically — they implement `IParsable<TSelf>` and the
framework's built-in `TryParse` model binder picks them up. Two wrappers
need a custom binder:

- `NonEmptyEnumerable<T>` — the wire form is *multiple* strings
  (`?xs=1&xs=2&xs=3`), not one. The binder delegates to the framework's
  `T[]` binder for the elements and then enforces the non-empty
  invariant; an empty source surfaces as a 400 with
  `ValidationProblemDetails`.
- `Maybe<T>` in form-bound patch contracts — `Maybe<T>?` can distinguish
  an omitted field (`null`, skip), an empty field (`None`, clear), and a
  non-empty field (`Some(parse(s))`, set). Query strings and headers can
  technically represent some of those shapes, but they make poor public
  contracts; prefer `T?` there.

`[FromBody]` round-tripping for both types already works through the
JSON converters that ship with `Kalicz.StrongTypes` — this package
covers only the non-body sources.

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

## Example

```csharp
[HttpGet("search")]
public IActionResult Search(
    [FromQuery] NonEmptyEnumerable<NonEmptyString> tags,
    [FromQuery] NonEmptyEnumerable<Positive<int>> counts)
{
    // tags is guaranteed non-empty; the framework returns 400 + problem
    // details if the caller sent zero ?tags=… values, or any tag is
    // empty / whitespace-only.
    // counts is guaranteed non-empty and every entry > 0.
    return Ok(new { count = tags.Count, total = counts.Sum(c => c.Value) });
}

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

What's **not** supported is wrapper-of-wrapper —
`NonEmptyEnumerable<Maybe<T>>`, `Maybe<NonEmptyEnumerable<T>>`,
`NonEmptyEnumerable<NonEmptyEnumerable<T>>`. These have no obvious wire
form on a non-body source (how would you encode "list of optional
lists" in `?foo=…` syntax?), and they'd need real model-binder
composition rather than per-element string parsing. If you need that
shape, use `[FromBody]` — the JSON converters that ship with
`Kalicz.StrongTypes` handle arbitrary nesting through the JSON
serializer.

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
