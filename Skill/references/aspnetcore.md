# ASP.NET Core MVC binding — `Kalicz.StrongTypes.AspNetCore`

A small, niche companion package. **Most ASP.NET Core apps don't need
it.** Reach for it only when:

- A controller action takes `Maybe<T>` or `NonEmptyEnumerable<T>` from
  `[FromForm]` (the primary use), `[FromQuery]`, `[FromHeader]`, or
  `[FromRoute]`.

If the app talks JSON over `[FromBody]`, this package adds nothing —
the JSON converters in the core `Kalicz.StrongTypes` package already
handle every wrapper, with arbitrary nesting. Don't recommend
installing it for JSON APIs.

## When you DO need it

Two specific shapes that the framework's built-in binders can't
produce on their own:

1. **`Maybe<T>?` on a form-posted patch contract.** A single HTML
   form field that needs three intents — *don't touch*, *clear*,
   *set* — can't be expressed with `T?` alone. With this package,
   `Maybe<T>?` binds: `null` = field omitted, `None` = field present
   but empty, `Some(value)` = field set to a parsed value.

   ```csharp
   public sealed record ProfilePatch(Maybe<NonEmptyString>? DisplayName);

   [HttpPost("profile")]
   public IActionResult Patch([FromForm] ProfilePatch patch) { ... }
   ```

2. **`NonEmptyEnumerable<T>` from repeated form fields or query
   parameters.** Multi-select inputs, checkbox groups, list-style
   filters (`?tags=a&tags=b&tags=c`). The binder enforces the
   non-empty invariant; an empty / missing source surfaces as a 400
   with `ValidationProblemDetails`.

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

- **JSON APIs.** `[FromBody]` round-trips both `Maybe<T>` and
  `NonEmptyEnumerable<T>` (and arbitrary nesting of them) via the
  JSON converters in the core package. No extra reference required.
- **Single-string strong types from non-body sources.** If the action
  signature is `[FromQuery] NonEmptyString` or
  `[FromRoute] Positive<int>`, ASP.NET Core's built-in `TryParse`
  binder already handles it — `IParsable<TSelf>` is enough.
- **Three-state semantics on a query string or header.** The binder
  *will* bind `Maybe<T>` from those sources, but the third state
  (omitted vs. present-but-empty) doesn't really survive on those
  wire formats. Use `T?` instead — the distinction isn't
  controllable by the caller.

## Wiring

One call on the service collection:

```csharp
builder.Services.AddControllers();
builder.Services.AddStrongTypes();          // from StrongTypes.AspNetCore
```

`AddStrongTypes()` inserts both `IModelBinderProvider`s at the front
of `MvcOptions.ModelBinderProviders`, ahead of the framework's
collection / simple-type providers.

## Element type support

Both binders parse each raw string via `IParsable<T>`. Element types
that work:

- BCL primitives that implement `IParsable<T>` — `int`, `long`,
  `Guid`, `DateTime`, `decimal`, `TimeSpan`, …
- Every `Kalicz.StrongTypes` wrapper — invariant violations
  (e.g. `Positive<int>` on `0`, malformed `Email`) surface as 400 +
  `ValidationProblemDetails`, with the failing field named in
  `ModelState`.

**Not supported** — wrapper-of-wrapper combinations on non-body
sources: `NonEmptyEnumerable<Maybe<T>>`,
`Maybe<NonEmptyEnumerable<T>>`,
`NonEmptyEnumerable<NonEmptyEnumerable<T>>`. There's no clean wire
form for them on a query string / header / form. Use `[FromBody]` if
you need that nesting.

## Decision rule

> **Default: don't add this package.** Only add it when a controller
> action needs `Maybe<T>` or `NonEmptyEnumerable<T>` from a non-body
> source — and that's not just a workaround for "I want a strong
> type in my query string." For single-value wrappers from query /
> route / header, the framework already handles them; for JSON APIs,
> `[FromBody]` already handles them.
