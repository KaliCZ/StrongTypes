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

## `null` and `""` — the exact matrix

Not uniform, and not guessable. Measured; `ConfigurationBinder.Get<T>` and
`IOptions<T>.Value` agree on **every** row:

| in `appsettings.json` | `NonEmptyString` | `NonEmptyString?` | `Positive<int>` | `Positive<int>?` |
| --------------------- | ---------------- | ----------------- | --------------- | ---------------- |
| key absent            | default kept     | `null`            | `1` (`default`) | `null`           |
| `null`                | **`null`**       | `null`            | `1` (`default`) | `null`           |
| `""`                  | **throws**       | **throws**        | **throws**      | **`null`**       |
| `"  "`                | **throws**       | **throws**        | throws (format) | throws (format)  |
| valid                 | binds            | binds             | binds           | binds            |
| invariant breach      | throws           | throws            | throws          | throws           |

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
