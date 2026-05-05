# ASP.NET Core MVC binding — `Kalicz.StrongTypes.AspNetCore`

A small, niche companion package. **Most ASP.NET Core apps don't need
it.** Reach for it only when:

- A controller action takes `NonEmptyEnumerable<T>` from `[FromForm]`,
  `[FromQuery]`, `[FromHeader]`, or `[FromRoute]`.

If the app talks JSON over `[FromBody]`, this package adds nothing —
the JSON converters in the core `Kalicz.StrongTypes` package already
handle every wrapper, with arbitrary nesting. Don't recommend
installing it for JSON APIs.

## When you DO need it

**`NonEmptyEnumerable<T>` from repeated form fields, query
parameters, or header lists.** Multi-select inputs, checkbox groups,
list-style filters (`?tags=a&tags=b&tags=c`). The binder enforces the
non-empty invariant; an empty / missing source surfaces as a 400 with
`ValidationProblemDetails`.

```csharp
[HttpPost("articles")]
public IActionResult Create(
    [FromForm] NonEmptyEnumerable<NonEmptyString> tags) { ... }
```

Every other strong-type wrapper (`NonEmptyString`, `Email`, `Digit`,
`Positive<T>`, `Negative<T>`, `NonNegative<T>`, `NonPositive<T>`)
binds from every non-body source **without this package**, because
they implement `IParsable<TSelf>` and ASP.NET Core's built-in
`TryParse` binder picks them up.

## When you DO NOT need it

- **JSON APIs.** `[FromBody]` round-trips `NonEmptyEnumerable<T>`
  (and arbitrary nesting with `Maybe<T>`) via the JSON converters in
  the core package. No extra reference required.
- **Single-string strong types from non-body sources.** If the action
  signature is `[FromQuery] NonEmptyString` or
  `[FromRoute] Positive<int>`, ASP.NET Core's built-in `TryParse`
  binder already handles it — `IParsable<TSelf>` is enough.

## `Maybe<T>` is not supported on non-body slots

`Maybe<T>` is **intentionally unsupported** on `[FromQuery]`,
`[FromRoute]`, `[FromHeader]`, and `[FromForm]`. The wire formats for
those slots only model "present" vs "absent" — there's no
protocol-level "explicitly null" — so the three-state semantic that
motivates `Maybe<T>?` collapses to two-state, which `T?` already
covers natively.

- **Optional non-body field?** Use `T?` (e.g. `[FromQuery] int? age`,
  `[FromForm] NonEmptyString? nickname`). The framework binds
  "absent" to `null` and "present" to the parsed value.
- **Three-state PATCH semantics?** Use `Maybe<T>?` from `[FromBody]`
  with a JSON payload. The JSON converter distinguishes "property
  omitted" (`null`), "explicit null" (`None`), and "value supplied"
  (`Some`). That's the only wire format where all three are
  unambiguously expressible.

If a project today has `[FromForm] Maybe<T>` or `[FromQuery] Maybe<T>`
parameters, switch them to `T?` (or move the contract to `[FromBody]`
if real PATCH semantics are needed).

## Wiring

One call on the service collection:

```csharp
builder.Services.AddControllers();
builder.Services.AddStrongTypes();          // from StrongTypes.AspNetCore
```

`AddStrongTypes()` inserts the `NonEmptyEnumerable<T>`
`IModelBinderProvider` at the front of
`MvcOptions.ModelBinderProviders`, ahead of the framework's
collection providers.

## Element type support

The binder parses each raw string via `IParsable<T>`. Element types
that work:

- BCL primitives that implement `IParsable<T>` — `int`, `long`,
  `Guid`, `DateTime`, `decimal`, `TimeSpan`, …
- Every `Kalicz.StrongTypes` wrapper — invariant violations
  (e.g. `Positive<int>` on `0`, malformed `Email`) surface as 400 +
  `ValidationProblemDetails`, with the failing field named in
  `ModelState`.

**Not supported** —
`NonEmptyEnumerable<NonEmptyEnumerable<T>>` on non-body sources.
There's no clean wire form for nested collections on a query string /
header / form. Use `[FromBody]` if that nesting is needed.

## Decision rule

> **Default: don't add this package.** Only add it when a controller
> action needs `NonEmptyEnumerable<T>` from a non-body source — and
> that's not just a workaround for "I want a strong type in my query
> string." For single-value wrappers from query / route / header, the
> framework already handles them; for JSON APIs, `[FromBody]` already
> handles `NonEmptyEnumerable<T>` and `Maybe<T>` (including
> three-state PATCH).
