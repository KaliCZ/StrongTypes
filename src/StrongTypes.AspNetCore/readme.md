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
    [FromQuery] Maybe<int> filter)
{
    // ids is guaranteed non-empty; the framework returns 400 + problem
    // details if the caller sent zero ?ids=… values.
    // filter is None when the caller omits ?filter=…, Some(value) otherwise.
    return Ok(new { count = ids.Count, hasFilter = filter.IsSome });
}
```

## Supported element types

Both binders parse each raw string via the element type's
`TypeConverter`. That covers every type the BCL ships a converter for —
`int`, `long`, `Guid`, `DateTime`, `string`, the other primitives, and
any user type that registers its own `[TypeConverter(...)]`.

It does **not** cover the strong-type wrappers in this library
(`NonEmptyString`, `Email`, `Digit`, `Positive<T>`, the other numeric
wrappers). Those parse via `IParsable<T>`, which `TypeConverter` does
not consult. Today the supported shapes for non-body binding are:

- ✅ `NonEmptyEnumerable<T>` where `T` is a primitive / BCL type
  (`NonEmptyEnumerable<int>`, `NonEmptyEnumerable<Guid>`, …).
- ✅ `Maybe<T>` where `T` is a primitive / BCL type.
- ❌ `NonEmptyEnumerable<StrongType>` and `Maybe<StrongType>` for the
  wrapper types — wire-form parsing isn't wired through `IParsable<T>`
  yet.
- ❌ Wrapper-of-wrapper (`NonEmptyEnumerable<Maybe<…>>`,
  `Maybe<NonEmptyEnumerable<…>>`) — would need real model-binder
  composition rather than per-element string parsing.

For the wrapper types, `[FromBody]` already round-trips correctly via
the JSON converters that ship with `Kalicz.StrongTypes` — only the
non-body sources are gapped.

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
