# Configuration and options binding

Strong types bind from `IConfiguration` / `IOptions<T>` with **no setup** —
every scalar wrapper carries a `TypeConverter`, which is what `ConfigurationBinder`
uses to turn a config string into a typed value. Put the wrapper straight on
the options class:

```csharp
public sealed class RetryOptions
{
    public Positive<int> MaxRetries { get; set; }
    public NonNegative<int> InitialDelaySeconds { get; set; }
    public NonEmptyString? Name { get; set; }
    public Email? Contact { get; set; }
    public Positive<int>? OptionalLimit { get; set; }   // absent key stays null
}

builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection("Retry"))
    .ValidateOnStart();                                  // see "Fail at startup" below
```

```json
{
  "Retry": {
    "MaxRetries": 5,
    "InitialDelaySeconds": 0,
    "Name": "checkout",
    "Contact": "ops@example.com"
  }
}
```

## The invariant is the validation rule

This is the reason to use a wrapper here rather than a plain `int`. The type
already says "must be positive", so a bad value is rejected without a
`[Range]` attribute or a custom `IValidateOptions<T>`:

```
InvalidOperationException: Failed to convert configuration value '-5' at 'Retry:MaxRetries'
                           to type 'StrongTypes.Positive`1[System.Int32]'.
  ---> ArgumentException: Value must be positive, but was '-5'. (Parameter 'value')
```

The outer frame names the **config path** and the **offending value**; the
inner one names the **broken invariant**. Don't add a `[Range(1, int.MaxValue)]`
on top of a `Positive<int>` — it is the same rule stated twice, and only one
of the two is enforced by the type.

## Fail at startup, not on the first request

Options binding is **lazy**. Without `ValidateOnStart()`, a bad value does not
stop the app: it starts, serves traffic, and throws on the first request that
reads `IOptions<T>.Value` — surfacing as a 500 rather than a failed deploy.

```csharp
builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection("Retry"))
    .ValidateOnStart();     // conversion failures now abort startup
```

`ValidateOnStart()` forces eager binding, so it catches conversion failures
even with **no** `IValidateOptions<T>` registered. Always add it when options
hold strong types — otherwise the invariant buys you a later crash, not an
earlier one.

## The missing key — `BindStrongTypes()`

Add [`Kalicz.StrongTypes.Configuration`](https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration/)
and the declaration becomes the spec — no `[Required]`, no `ValidateDataAnnotations()`:

```csharp
builder.Services.AddOptions<ClientOptions>()
    .BindStrongTypes(builder.Configuration.GetSection("Client"))
    .ValidateOnStart();
```

It fails when a property **declared non-nullable is null after binding** — the one state an absent
key can leave that the declaration forbids. Your nullable annotations already say which properties
those are, so no `[Required]` has to repeat it:

```csharp
public sealed class ClientOptions
{
    public NonEmptyString Name { get; set; } = null!;          // fails unless configured
    public NonEmptyString? Nickname { get; set; }              // nullable — fine
    public string ApiKey { get; set; } = null!;                // fails unless configured
    public string Endpoint { get; set; } = "https://x.test";   // has a default — never null
    public Positive<int> MaxRetries { get; set; }              // never checked — see below
}
```

```
OptionsValidationException: 'Client:Name' is null. Configure it, give ClientOptions.Name a default,
                            or declare it nullable.
```

**Every reference property is covered, not only the wrappers** — a non-nullable `string` is as
broken by a missing key as a `NonEmptyString`. A declared default needs nothing: it isn't null.

**At every depth, not just the top level.** Nested options classes, collection elements and
dictionary values are all walked, and the failure names the full path (`'Client:Endpoints:1:Host'`).
The walk stops exactly where `ConfigurationBinder` stops — at a type it has a `TypeConverter` for —
so a wrapper is a leaf, never something to recurse into.

**Value types are not checked, and don't need to be.** An unconfigured `Positive<int>` is `1` and an
unconfigured `bool` is `false` — defaults, and values those types are happy to hold. There is no
contradiction to catch, so this will not tell you that you forgot `MaxRetries`. Declare it
`Positive<int>?` if "not configured" must be distinguishable; that is the only way the CLR offers.

An options class in an assembly with `<Nullable>disable</Nullable>` declares no intent, so nothing
on it is enforced.

Analyzer **ST0004** flags a plain `Bind` / `Configure` that would leave a non-nullable wrapper null,
with a code fix that rewrites a `Bind` call (a flagged `Configure` gets the diagnostic only). It reports only this library's own wrappers, never a plain
`string`, so it sees less than the check it points you at. It also stays quiet for a property
already carrying `[Required]`, which genuinely covers that case.

The rest of this section describes what happens **without** that package.

## `ValidateOnStart()` does not catch a *missing* value

It only surfaces failures that binding itself raises. A key that simply isn't
there raises nothing — binding succeeds, it just doesn't assign — so the
property keeps whatever the options class gave it. Since a real options class
has no initialisers, that means:

| declaration                 | key not in config | is that a problem?                        |
| --------------------------- | ----------------- | ----------------------------------------- |
| `NonEmptyString Name`       | **`null`**        | **yes** — the type says it is never null   |
| `Positive<int> MaxRetries`  | `1` (default)     | no — a value the type is happy to hold    |
| `Positive<int>? MaxRetries` | `null`            | no — it is nullable                       |

**A non-nullable `NonEmptyString` can be null.** The invariant constrains every
value the type can hold; it cannot make the binder assign one. Against an
unconfigured key the wrapper is no better than `string`, and that is the only
place an absent key produces a state the declaration forbids.

Guard it with `[Required]` and `ValidateDataAnnotations()` — or with
`BindStrongTypes()` above, which reads the same intent from the nullable
annotation instead of an attribute:

```csharp
builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection("Retry"))
    .ValidateDataAnnotations()   // [Required] → catches a null reference property
    .ValidateOnStart();          // → catches an invalid value, at startup
```

Neither can help with a struct: `default(Positive<int>)` is `1`, so nothing
distinguishes "configured as 1" from "never configured". Declare it
`Positive<int>?` when that distinction matters — the CLR offers no other way
to say it.

## `null` and `""` — the exact matrix

Not uniform, and not guessable. Measured; `ConfigurationBinder.Get<T>` and
`IOptions<T>.Value` agree on **every** row:

| in `appsettings.json` | `NonEmptyString` | `NonEmptyString?` | `Positive<int>` | `Positive<int>?` |
| --------------------- | ---------------- | ----------------- | --------------- | ---------------- |
| key absent            | **`null`** †     | `null`            | `1` (`default`) | `null`           |
| `null`                | **`null`**       | `null`            | `1` (`default`) | `null`           |
| `""`                  | **throws**       | **throws**        | **throws**      | **`null`**       |
| `"  "`                | **throws**       | **throws**        | throws (format) | throws (format)  |
| valid                 | binds            | binds             | binds           | binds            |
| invariant breach      | throws           | throws            | throws          | throws           |

† unless the options class initialises the property, which is rare — an absent
key leaves whatever was already there, and for a reference type that is `null`.

Three things in there surprise people:

- **`""` is not uniform.** It throws for everything *except* a nullable **struct**
  wrapper, where it binds to `null` — a nullable struct resolves to the BCL's
  `NullableConverter`, which maps empty to null before our converter is consulted.
  A nullable *reference* wrapper gets no such treatment (`NonEmptyString?` is the
  same runtime type as `NonEmptyString`), so `""` reaches `Parse` and fails.
  Don't reach for `""` to mean "unset" — omit the key.
- **An explicit `null` nulls even a non-nullable reference property.** Nullability
  is erased by the time the binder runs, so `"Name": null` leaves a
  `NonEmptyString Name` holding `null`, exactly as it would a `string` — which is
  precisely what `BindStrongTypes` (above) catches, treating it the same as an
  absent key. Without that package, omit the key rather than writing `null`.
- **An explicit `null` overwrites, an absent key does not.** `"Name": null` clears
  a property initialised in the options class; leaving the key out keeps it.

## Things worth knowing

- **A missing key leaves the default**, it does not throw. `default(Positive<int>)`
  is `1` and `default(NonNegative<int>)` is `0` — both satisfy their invariant, so
  a typo'd key is a silently valid default. Use `Positive<int>?` when "not
  configured" must be distinguishable from a configured value.
- **`ConfigurationBinder` parses with the invariant culture**, so `"1234.5"` is
  a `Positive<decimal>` of 1234.5 regardless of the host's locale. Config files
  are not localized; don't write `1234,5`.
- **Nullable wrappers still enforce the invariant** when a value is present:
  `OptionalLimit` may be absent, but `-1` is rejected.
- The same `TypeConverter` powers anything that goes through `TypeDescriptor` —
  WPF/WinForms two-way binding (`references/desktop.md`), designers,
  `PropertyGrid`, and libraries doing generic string↔object conversion. It is
  one mechanism on the type, so none of them need a registration call.
