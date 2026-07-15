# Configuration and options binding

Strong types bind from `IConfiguration` / `IOptions<T>` with **no setup** —
every wrapper carries a `TypeConverter`, which is what `ConfigurationBinder`
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
builder.Services.AddOptions<RetryOptions>()
    .BindStrongTypes(builder.Configuration.GetSection("Retry"))
    .ValidateOnStart();
```

```
OptionsValidationException: 'Retry:MaxRetries' is required but was not configured.
                            Declare RetryOptions.MaxRetries nullable if it is optional.
```

It works where `[Required]` cannot because it asks **configuration** whether the key is present,
rather than asking the bound object whether it looks null — and `default(Positive<int>)` is `1`,
which looks like a configured value.

A property is **required when it is not nullable and the options class gives it no default**:

```csharp
public NonEmptyString Name { get; set; } = null!;          // required — null! declares no default
public NonEmptyString? Nickname { get; set; }              // optional — nullable
public Positive<int> MaxRetries { get; set; }              // required
public string Endpoint { get; set; } = "https://x.test";   // optional — has a default
public int? Timeout { get; set; }                          // optional
```

**Every property is checked, not only the wrappers** — opting in says the class should be fully
configured, and a missing `string` is as silent as a missing `NonEmptyString`. Two declarations
cannot be read: a value type whose intended default *is* the CLR default
(`bool Enabled { get; set; } = false`) is required, since it is indistinguishable from having no
initialiser — declare it `bool?`; and a reference property in an assembly with
`<Nullable>disable</Nullable>` carries no annotation and is treated as optional.

Analyzer **ST0004** flags a plain `Bind` / `Configure` on an options type that needs this, with a
code fix that rewrites the call. It stays quiet for a reference wrapper already carrying
`[Required]`, since that case genuinely is covered.

The rest of this section describes what happens **without** that package.

## `ValidateOnStart()` does not catch a *missing* value

It only surfaces failures that binding itself raises. A key that simply isn't
there raises nothing — binding succeeds, it just doesn't assign — so the
property keeps whatever the options class gave it. Since a real options class
has no initialisers, that means:

| declaration                     | key not in config | `[Required]` catches it? |
| ------------------------------- | ----------------- | ------------------------ |
| `NonEmptyString Name`           | **`null`**        | yes                      |
| `Positive<int> MaxRetries`      | **`1`** (default) | **no**                   |
| `Positive<int>? MaxRetries`     | `null`            | yes                      |

Two consequences worth internalising:

- **A non-nullable `NonEmptyString` can be null.** The invariant constrains
  every value the type can hold; it cannot make the binder assign one. The
  wrapper is no better than `string` at surviving an unconfigured key.
- **A non-nullable struct wrapper cannot be checked at all.**
  `default(Positive<int>)` is `1` — a real, invariant-satisfying value — so
  `[Required]` passes and nothing distinguishes "configured as 1" from "never
  configured". **Declare it `Positive<int>?` when that distinction matters.**

So the full guard is both:

```csharp
builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection("Retry"))
    .ValidateDataAnnotations()   // [Required] → catches a missing value
    .ValidateOnStart();          // → catches an invalid one, at startup
```

…with `[Required]` on each property that must be present, and struct wrappers
declared nullable so `[Required]` has a null to find.

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
  `NonEmptyString Name` holding `null`, exactly as it would a `string`. The
  invariant constrains every value the type can hold; it cannot stop the binder
  assigning none. Omit the key rather than writing `null`.
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
  WPF/WinForms two-way binding (`references/wpf.md`), designers,
  `PropertyGrid`, and libraries doing generic string↔object conversion. It is
  one mechanism on the type, so none of them need a registration call.
