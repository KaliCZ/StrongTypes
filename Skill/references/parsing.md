# Parsing helpers

Three small surfaces: `Digit`, enum extension members, and string parsers.
All three follow the `As… returns T?` / `To… throws` pattern.

## `Digit`

A `readonly struct` wrapping a single `'0'`–`'9'` character.

```csharp
Digit? d = Digit.TryCreate('7');   // null if not '0'..'9'
Digit  d = Digit.Create('7');      // throws on invalid

Digit? d = '7'.AsDigit();          // extension on char
```

Conversions and invariants:

- `Value` — the underlying `byte` (0–9).
- Implicit `Digit → byte` and `Digit → int` — drop a `Digit` into any
  numeric slot without `.Value`.
- `IEquatable` / `IComparable` against `Digit`, `byte`, and `int`.
- `default(Digit)` is `0`.

Also a string helper for extracting digits:

```csharp
IEnumerable<Digit> digits = "a1b2c3".FilterDigits();   // Digit 1, 2, 3
```

Use `Digit` for parsing character streams (phone numbers, postal codes,
VINs) where each character must be a digit and the downstream code
benefits from that being typed.

## Enum extensions

`EnumExtensions` adds methods directly onto the enum type. You call
`Roles.Parse(...)`, not `EnumExtensions.Parse<Roles>(...)`.

```csharp
[Flags]
public enum Roles { None = 0, Reader = 1, Writer = 2, Admin = 4 }

// Factories — BCL-style naming
Roles  r1 = Roles.Parse("Reader");                    // throws on failure
Roles  r2 = Roles.Parse("reader", ignoreCase: true);
Roles? r3 = Roles.TryParse(userInput);                // null on failure
Roles? r4 = Roles.TryParse(userInput, ignoreCase: true);

// Same under StrongTypes' Create / TryCreate naming
Roles  r5 = Roles.Create("Reader");
Roles? r6 = Roles.TryCreate(userInput);

// All declared members (cached — safe in hot paths)
IReadOnlyList<Roles> all = Roles.AllValues;           // [None, Reader, Writer, Admin]
```

For `[Flags]` enums:

```csharp
IReadOnlyList<Roles> flags = Roles.AllFlagValues;     // [Reader, Writer, Admin] (excludes 0 and composites)
Roles                super = Roles.AllFlagsCombined;  // Reader | Writer | Admin

// Decompose a combined value back into declaration-order flags.
foreach (var flag in (Roles.Reader | Roles.Admin).GetFlags()) { ... }
```

`AllFlagValues`, `AllFlagsCombined`, and `GetFlags()` throw
`InvalidOperationException` if the enum is not `[Flags]` — so a typo at
declaration fails on first use, not silently.

There's also a typed converter pair for generic code:

```csharp
long raw   = EnumExtensions<Roles>.ToLong(value);
Roles back = EnumExtensions<Roles>.FromLong(raw);
```

## String parsers

The open-generic enum variant (useful when you only know the enum type
via a generic parameter, where `Roles.TryParse(...)` isn't reachable):

```csharp
TEnum? e1 = input.AsEnum<TEnum>();                // null on failure
TEnum  e2 = input.ToEnum<TEnum>();                // throws on failure
TEnum? e3 = input.AsEnum<TEnum>(ignoreCase: true);
```

Full set on `string?` (all return `T?` / throw):

| `As…` (nullable)            | `To…` (throwing)             |
| --------------------------- | ---------------------------- |
| `AsNonEmpty()`              | `ToNonEmpty()`               |
| `AsByte()`                  | `ToByte()`                   |
| `AsShort()`                 | `ToShort()`                  |
| `AsInt()`                   | `ToInt()`                    |
| `AsLong()`                  | `ToLong()`                   |
| `AsFloat()`                 | `ToFloat()`                  |
| `AsDouble()`                | `ToDouble()`                 |
| `AsDecimal()`               | `ToDecimal()`                |
| `AsBool()`                  | `ToBool()`                   |
| `AsDateTime()`              | `ToDateTime()`               |
| `AsTimeSpan()`              | `ToTimeSpan()`               |
| `AsGuid()`                  | `ToGuid()`                   |
| `AsGuidExact(format)`       | `ToGuidExact(format)`        |
| `AsEnum<TEnum>()`           | `ToEnum<TEnum>()`            |

Numeric parsers accept optional `IFormatProvider` and `NumberStyles`.
Date/time parsers accept `IFormatProvider` and `DateTimeStyles`. The same
extensions are available on `NonEmptyString` (calling them with `.Value`
is unnecessary).

## Common pattern

```csharp
public IActionResult Get([FromQuery] string? id, [FromQuery] string? sort)
{
    if (id.AsGuid() is not { } guid)             return BadRequest("id must be a GUID");
    if (sort.AsEnum<SortOrder>() is not { } so)  return BadRequest("invalid sort");

    return Ok(_service.List(guid, so));
}
```
