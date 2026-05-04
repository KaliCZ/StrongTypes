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
- `Maybe<T>` — *missing* maps to `None`, *present* maps to
  `Some(parse(s))`. `IParsable<T>.TryParse` can't model the missing
  case because there's no string to parse. The binder reads the value
  provider directly to detect absence and delegates to the framework's
  binder for `T` when a value is present.

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
    [FromQuery] NonEmptyEnumerable<int> ids,
    [FromQuery] Maybe<NonEmptyString> filter)
{
    // ids is guaranteed non-empty; the framework returns 400 + problem
    // details if the caller sent zero ?ids=… values.
    // filter is None when the caller omits ?filter=…, Some(value) otherwise.
    return Ok(new { count = ids.Count, hasFilter = filter.IsSome });
}
```

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
