# ASP.NET Core MVC integration — `Kalicz.StrongTypes.AspNetCore`

A small companion package. Reach for it when either applies:

- A controller action takes `NonEmptyEnumerable<T>` from `[FromForm]`,
  `[FromQuery]`, `[FromHeader]`, or `[FromRoute]` (the niche binder).
- You want **JSON request-body validation error keys normalized** so a
  failed strong type is reported under the property name (`Value`)
  instead of the System.Text.Json path (`$.value`) — matching the keys
  data-annotation and model-binding errors use.

The binder is niche: for `[FromBody]` the core `Kalicz.StrongTypes`
JSON converters already round-trip every wrapper with arbitrary
nesting, so you don't need this package just to *bind* JSON. But the
error-key normalization is a JSON-API concern, and it's the reason a
pure JSON API might still want this package.

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

- **JSON APIs that only need binding.** `[FromBody]` round-trips
  `NonEmptyEnumerable<T>` (and arbitrary nesting with `Maybe<T>`) via
  the JSON converters in the core package. No extra reference required
  to *bind*. (If you also want the validation error keys normalized,
  see "JSON error-key normalization" below — that's a reason to add it.)
- **Single-string strong types from non-body sources.** If the action
  signature is `[FromQuery] NonEmptyString` or
  `[FromRoute] Positive<int>`, ASP.NET Core's built-in `TryParse`
  binder already handles it — `IParsable<TSelf>` is enough.

## `Maybe<T>` is not supported on non-body slots

`Maybe<T>` is **intentionally unsupported** on `[FromQuery]`,
`[FromRoute]`, `[FromHeader]`, and `[FromForm]`. Those wire formats
model "present" vs "absent" only, so the three-state semantic
collapses to two-state — `T?` covers it. Use `Maybe<T>?` from
`[FromBody]` when real three-state PATCH semantics are needed; the
JSON converter handles it.

## Wiring

One call on the service collection:

```csharp
builder.Services.AddControllers();
builder.Services.AddStrongTypes();          // from StrongTypes.AspNetCore
```

`AddStrongTypes()` does two things:

1. Inserts the `NonEmptyEnumerable<T>` `IModelBinderProvider` at the
   front of `MvcOptions.ModelBinderProviders`, ahead of the framework's
   collection providers.
2. Normalizes JSON request-body validation error keys (see below).

Configure via the overload:

```csharp
builder.Services.AddStrongTypes(o =>
{
    o.NormalizeJsonErrorKeys = true;                 // default; set false to opt out
    o.JsonErrorKeyCasing = JsonErrorKeyCasing.PascalCase; // default
});
```

## JSON error-key normalization

When a strong type fails to deserialize from a JSON request body, the
System.Text.Json input formatter keys the `ValidationProblemDetails`
error by the **JSON path** — `$.value`, `$.items[0].name` — whereas
data-annotation and model-binding errors key by the **property name**
(`Value`). `AddStrongTypes()` reconciles them: it rewrites the
`$.`-prefixed body keys to the property-name form so one API surface
reports one key convention.

- **Opt-out, on by default.** Set `NormalizeJsonErrorKeys = false` to
  keep the raw `$.value` paths.
- **Scope.** Affects only the automatic `[ApiController]`
  `ValidationProblemDetails` response. It does **not** touch
  System.Text.Json itself, raw `JsonSerializer` calls, or minimal-API
  binding, and model-binding errors (no `$.` prefix) pass through
  untouched. It rewrites *every* JSON-body error key, not only
  strong-type ones — a malformed `int` body key is normalized too.
- **Casing** (`JsonErrorKeyCasing`): `PascalCase` (default — matches
  the C# property name that data annotations use by default),
  `CamelCase`, or `StripOnly` (just drop the `$.` prefix, keep the
  wire casing). Pick the one matching your app's existing validation
  key convention. The casing is applied per path segment; a custom
  `[JsonPropertyName]` that isn't just a re-cased property name can't
  be recovered from the path.

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

> Add this package when **either**: a controller action needs
> `NonEmptyEnumerable<T>` from a non-body source (not just as a
> workaround for "I want a strong type in my query string"), **or** you
> want JSON request-body validation errors keyed by property name
> instead of the `$.path`. For single-value wrappers from query / route
> / header, the framework already binds them; for `[FromBody]`, the
> core converters already round-trip `NonEmptyEnumerable<T>` and
> `Maybe<T>` (including three-state PATCH) — so if you don't care about
> the error-key shape, you don't need it.
