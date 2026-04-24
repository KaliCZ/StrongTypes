## `NonEmptyString`

Wraps a `string` that is non-null, non-empty, and not whitespace-only.
Construction is always via the factory pair — constructors are private.

### Factories

```csharp
NonEmptyString? name = NonEmptyString.TryCreate(input);   // null when null / empty / whitespace
NonEmptyString  name = NonEmptyString.Create(input);      // throws ArgumentException

// Extensions — identical semantics, nicer syntax at the call site.
NonEmptyString? name = input.AsNonEmpty();                // null on failure
NonEmptyString  name = input.ToNonEmpty();                // throws on failure
```

`AsNonEmpty()` is the one you want 90% of the time — pattern-match with
`is not { } name` and fall through to a 400 response / early return.

### String-like surface

`NonEmptyString` exposes enough of the `string` API that you rarely need
to unwrap it. Returned types keep the invariant where it still holds —
`ToUpper()` returns `NonEmptyString`, but `Substring(...)` returns a
plain `string` because a substring could be empty.

- `Value` — the underlying `string`.
- `Length` — `int`.
- Case conversions returning `NonEmptyString`: `ToLower()`, `ToLower(CultureInfo)`,
  `ToLowerInvariant()`, `ToUpper()`, `ToUpper(CultureInfo)`, `ToUpperInvariant()`.
- Containment: `Contains(string)`, `Contains(string, StringComparison)`,
  `Contains(char)`, `Contains(char, StringComparison)`.
- Replacement returning `string`: `Replace(char, char)`, `Replace(string, string)`,
  `Replace(string, string, StringComparison)`.
- `Trim()` returning `string` (because trimming can empty the string).
- Indexing: `IndexOf` / `LastIndexOf` overloads for `string` and `char`,
  with optional `startIndex` and `StringComparison`.
- `Substring(int)`, `Substring(int, int)` returning `string`.
- `StartsWith(...)`, `EndsWith(...)` overloads mirroring `string`.
- Implicit conversion to `string` — you can pass a `NonEmptyString` to any
  `string` parameter without `.Value`.
- Full equality / comparison operators against `NonEmptyString` *and*
  `string` — no `.Value` needed for `==`, `!=`, `<`, `<=`, `>`, `>=`.

### `Unwrap()` — the EF-Core marker

```csharp
string raw = nonEmpty.Unwrap();
```

In-memory `Unwrap()` just returns `Value`. The interesting use case is
inside an EF Core LINQ predicate, where the EfCore package rewrites
`property.Unwrap()` into a direct column reference so string operators
(`StartsWith`, `EF.Functions.Like`, `Collate`, …) translate server-side.
Outside EF Core predicates, `Unwrap()` and `Value` are interchangeable —
pick whichever reads better.

### Parse extensions

Both `string?` and `NonEmptyString` have a set of nullable-returning `As…`
and throwing `To…` conversions:

```csharp
int?       id  = header.AsInt();
int        id  = header.ToInt();
decimal?   amt = body.AsDecimal();
DateTime?  t   = header.AsDateTime();
TimeSpan?  ts  = header.AsTimeSpan();
Guid?      g   = header.AsGuid();
Guid?      g2  = header.AsGuidExact("D");     // strict format
bool?      b   = flag.AsBool();
Roles?     r   = header.AsEnum<Roles>();
byte?      bt  = header.AsByte();
short?     sh  = header.AsShort();
long?      lg  = header.AsLong();
float?     f   = header.AsFloat();
double?    d   = header.AsDouble();
```

Each `AsX` has a `ToX` counterpart that throws `FormatException` /
`OverflowException` on failure. `As…`/`To…` on `string?` accept null
input and return null from `As…` or throw from `To…`.

### Typical patterns

```csharp
// Controller validation — unwrap with is not { } v
[HttpPost]
public IActionResult Create(CreateRequest request)
{
    if (request.Name.AsNonEmpty() is not { } name)
        return BadRequest("name required");
    if (request.Age.AsPositive() is not { } age)
        return BadRequest("age must be positive");

    _service.Create(name, age);
    return NoContent();
}

// Interop with string APIs — implicit conversion handles it.
string json = JsonSerializer.Serialize(name);      // NonEmptyString → string implicit

// Keep invariants on a record.
public record User(NonEmptyString Name, NonEmptyString? Nickname);
```

### JSON

`[JsonConverter(typeof(NonEmptyStringJsonConverter))]` is attached on the
type. Serialises as a plain JSON string. Deserialising `""`, a
whitespace-only string, or `null` (into the non-nullable form) throws
`JsonException`. No `JsonSerializerOptions` registration required.
