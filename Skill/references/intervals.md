# Interval types — `FiniteInterval<T>`, `Interval<T>`, `IntervalFrom<T>`, `IntervalUntil<T>`

Four generic `readonly struct` types holding an ordered pair of endpoints with
the invariant `Start <= End` (checked whenever both endpoints are present).
`T` is any `struct, IComparable<T>` — `int`, `long`, `decimal`, `DateTime`,
`DateOnly`, `TimeOnly`, and so on.

Both bounds are **inclusive by default** — `[Start, End]`, so "June through
August" is `Create(june1, august31)`. Each bound can be excluded per value via
the optional `startInclusive` / `endInclusive` factory parameters:
`Create(9, 17, endInclusive: false)` is `[9, 17)`. Equal endpoints form a
**single-value interval** and require both bounds inclusive; an *empty*
interval is not constructible — model "no interval" as a nullable interval
that is `null`.

The variant fixes **which endpoints are bounded**, so the compiler — not a
runtime check — guarantees a bounded endpoint is never `null`. Pick the
variant by which ends are unbounded:

| Type                | `Start` | `End` | Meaning                       |
| ------------------- | ------- | ----- | ----------------------------- |
| `FiniteInterval<T>` | `T`     | `T`   | bounded both ends             |
| `IntervalFrom<T>`   | `T`     | `T?`  | "from X" — no upper bound   |
| `IntervalUntil<T>`  | `T?`    | `T`   | "until Y" — no lower bound  |
| `Interval<T>`       | `T?`    | `T?`  | either / both ends optional   |

Every variant's `default` satisfies its invariant (`default(FiniteInterval<int>)`
is the single-value `[0, 0]`; `default(Interval<int>)` is unbounded).

## Factories

Same `TryCreate` / `Create` split as the rest of the library. They reject
`Start > End` (only when both endpoints are present — an unbounded endpoint
can't violate ordering) and equal endpoints with an exclusive bound (the empty
interval). Construct through the static factories; these types have no
`AsX` / `ToX` extensions because they take two arguments. Each variant has a
non-generic companion class whose factories infer the endpoint type from the
arguments — spell the type argument out only when every argument is a `null`
literal:

```csharp
FiniteInterval<int>?  r  = FiniteInterval.TryCreate(1, 10);   // [1, 10], null if start > end
FiniteInterval<int>   r  = FiniteInterval.Create(1, 10);      // throws if start > end
FiniteInterval<int>   s  = FiniteInterval.Create(9, 17, endInclusive: false);   // [9, 17)

IntervalFrom<DateOnly>  fromToday = IntervalFrom.Create(today, null);  // open-ended
IntervalUntil<DateOnly> untilXmas = IntervalUntil.Create(null, xmas);  // through Christmas
Interval<int>           anything  = Interval.Create<int>(null, null);  // unbounded — nothing to infer from
```

The inclusivity of an **unbounded** endpoint is meaningless, so it normalizes
to `true`: `IntervalFrom.Create(1, null, endInclusive: false)` equals
`IntervalFrom.Create(1, null)` — no phantom state to compare or serialize.

## What you get on every variant

- `.Start` / `.End` — typed per the variant (bounded endpoints are `T`,
  optional ones `T?`).
- `.StartInclusive` / `.EndInclusive` — whether each bound is included;
  `true` unless the value was created with `startInclusive: false` /
  `endInclusive: false`. Part of equality.
- `Contains(T value)` — membership honoring each bound's inclusivity; an
  unbounded end accepts everything in that direction.
- `Overlaps(other)` / `GetOverlap(other)` — intersection with any variant
  (the parameter is `Interval<T>`, so the other three widen in). See below.
- `Deconstruct` — enables pattern matching. For `Interval<T>` this gives the
  exhaustive four-arm switch over the bound cases.
- `IEquatable<Self>`, `==` / `!=`, `GetHashCode`, `Equals(object?)`.
- `ToString()` — interval notation with brackets per bound, e.g. `[1, 10]`,
  `[9, 17)`, `(1, 10)`, `[1, +∞)`, `(-∞, 10]`, `(-∞, +∞)`.
- `System.Text.Json` converter via `[JsonConverter]`.
- Implicit widening to the less-constrained variants (see below).

```csharp
var interval = Interval.Create(start, end);

string label = interval switch
{
    (null, null)         => "unbounded",
    (null, { } e)        => $"up to {e}",
    ({ } s, null)        => $"from {s}",
    ({ } s, { } e)       => $"{s}..{e}",
};

bool open = IntervalFrom.Create(0, null).Contains(int.MaxValue);   // true
```

## Widening between variants

A more-constrained variant widens **implicitly** to a less-constrained one —
the conversion is total and lossless, so it never throws:

- `FiniteInterval<T>` → `IntervalFrom<T>`, `IntervalUntil<T>`, or `Interval<T>`
- `IntervalFrom<T>` → `Interval<T>`
- `IntervalUntil<T>` → `Interval<T>`

(There is no implicit `IntervalFrom` ↔ `IntervalUntil`: swapping which end is
open would require the optional end to be present, which isn't guaranteed.)

Because the conversion is implicit, this is also how you hold mixed variants in
one collection — widen them all to `Interval<T>`. These are distinct `struct`
types with no inheritance, but the operator runs on each element:

```csharp
Interval<int>[] windows =
[
    FiniteInterval.Create(0, 10),    // widened
    IntervalFrom.Create(5, null),    // widened
    Interval.Create<int>(null, null),
];

var matching = windows.Where(w => w.Contains(7));
```

`List<Interval<int>>` works the same way (`Add` takes `Interval<T>`). The
elements are stored inline as structs — no boxing. To recover "is this fully
bounded?" later, pattern-match the value (`w is ({ } s, { } e)`), which keys on
the actual endpoints rather than how the value was originally constructed.

Going the other way is **partial** — narrowing can fail when an endpoint that
the target requires is open — so it follows the library's `As…` convention and
returns a nullable, mirroring `TryCreate`:

| On | Method | `null` when |
| -- | ------ | ----------- |
| `Interval<T>` | `AsFinite()` → `FiniteInterval<T>?` | either endpoint unbounded |
| `Interval<T>` | `AsFrom()` → `IntervalFrom<T>?` | lower endpoint unbounded |
| `Interval<T>` | `AsUntil()` → `IntervalUntil<T>?` | upper endpoint unbounded |
| `IntervalFrom<T>` | `AsFinite()` → `FiniteInterval<T>?` | upper endpoint unbounded |
| `IntervalUntil<T>` | `AsFinite()` → `FiniteInterval<T>?` | lower endpoint unbounded |

```csharp
Interval<int> i = Interval.Create(1, 10);
FiniteInterval<int>? bounded = i.AsFinite();     // [1, 10]
FiniteInterval<int>? open    = IntervalFrom.Create(1, null).AsFinite();   // null
```

Widening and narrowing both carry the bound flags along unchanged.

Each `As…` has a `To…` sibling that throws `InvalidOperationException` instead
of returning `null` (the `Create`-style member of the pair) — `ToFinite()`,
`ToFrom()`, `ToUntil()`. Use it when an unbounded endpoint is a bug, not an expected
case.

## Overlap

Every variant answers `Overlaps` and computes `GetOverlap` against any other
variant — the parameter is `Interval<T>`, so the more-constrained variants
widen into it implicitly. Semantics follow `Contains`: intervals that touch at
a shared endpoint overlap in that single point when **both** touching bounds
are inclusive; make one of them exclusive (back-to-back time windows) and they
don't overlap. On a boundary tie, the intersection's bound is inclusive only
when both sides' bounds are.

`GetOverlap` returns `null` when the intervals are disjoint. Its return type
keeps the endpoints the receiver already guarantees — intersecting with a
`FiniteInterval<T>` can't unbound an endpoint, so it returns
`FiniteInterval<T>?`, while `IntervalFrom<T>` returns `IntervalFrom<T>?`,
`IntervalUntil<T>` returns `IntervalUntil<T>?`, and `Interval<T>` returns
`Interval<T>?`.

```csharp
var booked = FiniteInterval.Create(10, 20);
var query  = IntervalFrom.Create(15, null);

bool clash = booked.Overlaps(query);                     // true
FiniteInterval<int>? shared = booked.GetOverlap(query);  // [15, 20]
booked.GetOverlap(FiniteInterval.Create(30, 40));        // null — disjoint

FiniteInterval.Create(0, 5).GetOverlap(FiniteInterval.Create(5, 9));   // [5, 5] — inclusive bounds touch
var morning   = FiniteInterval.Create(9, 12, endInclusive: false);
var afternoon = FiniteInterval.Create(12, 17, endInclusive: false);
morning.Overlaps(afternoon);                             // false — [9, 12) stops short of noon
```

## Date bridging (`DateTime` ↔ `DateOnly`)

Cross-type `Contains` overloads connect instant intervals and date intervals:
a `DateTime` interval contains a `DateOnly` when it covers **any instant of
that calendar day** (an interval with an *exclusive* end at exactly midnight
stops short of the day it ends on), and a `DateOnly` interval contains a
`DateTime` by the moment's calendar day. `ToDateInterval()` converts a
`DateTime` interval to the calendar days it covers, as an inclusive date
interval — `interval.Contains(day)` and
`interval.ToDateInterval().Contains(day)` always agree; unbounded endpoints
stay unbounded. `Days()` counts the days a `FiniteInterval<DateOnly>`
contains (`[Jan 1, Jan 3]` is 3 days; an excluded endpoint day doesn't count);
on a `FiniteInterval<DateTime>` it counts the calendar days covered — the same
as `ToDateInterval().Days()`.
Going the other way, `DateOnly.ToTimeInterval()` expands a day to its instants as
a `FiniteInterval<DateTime>` (half-open, `[midnight, next midnight)`), and
`DateTime.ToDateOnly()` drops a moment's time of day.

```csharp
var stay = FiniteInterval.Create(new DateTime(2026, 7, 1, 14, 0, 0), new DateTime(2026, 7, 4, 10, 0, 0));

stay.Contains(new DateOnly(2026, 7, 4));     // true — the stay reaches into that day
stay.ToDateInterval();                       // FiniteInterval<DateOnly> [2026-07-01, 2026-07-04]
stay.ToDateInterval().Days();                // 4 — July 1 through July 4
stay.Days();                                 // 4 — Days() works straight on a DateTime interval too
new DateOnly(2026, 7, 4).ToTimeInterval();   // FiniteInterval<DateTime> [2026-07-04 00:00, 2026-07-05 00:00)

var season = FiniteInterval.Create(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31));   // June through August
season.Contains(new DateTime(2026, 8, 31, 9, 30, 0));   // true — the end day is inclusive
```

Comparisons are calendar-based and ignore `DateTimeKind` — bring both sides to
the same zone before bridging mixed-kind values.

## JSON

On **write**, an interval is always an object with both endpoint keys present
(an unbounded endpoint is `null`); the bound flags appear **only when
`false`**, so the common inclusive case stays clean:

```json
{ "Start": 1, "End": 10 }                            // FiniteInterval<int> [1, 10]
{ "Start": 9, "End": 17, "EndInclusive": false }     // [9, 17)
{ "Start": 1, "End": null }                          // IntervalFrom<int>, open-ended
{ "Start": null, "End": null }                       // Interval<int>, unbounded
```

On **read**, an absent key for an *optional* endpoint means `null` — so
`IntervalUntil` accepts `{ "End": 10 }` and `Interval` accepts `{}` — and an
absent bound flag means `true`. Property names honour the active
`JsonNamingPolicy` (camelCase under the web defaults). A payload is rejected
with `JsonException` (which ASP.NET Core surfaces as a `400` keyed by the
property path, `$.value`) when it violates the invariant (`Start > End`, or
equal endpoints with an exclusive bound), omits or nulls a *required*
endpoint, or isn't a JSON object. Equal endpoints with inclusive bounds are
valid and deserialize to a single-value interval.

### Pinning a bound's inclusivity

The default converter carries each bound per value (the `Stored`
`IntervalBoundMode`). To fix a bound for a specific property — mirroring the
EF `IntervalBoundMode` — apply an `IntervalJsonConverter<TInterval>` with the
two modes. An `AlwaysInclusive` / `AlwaysExclusive` bound is **never written**
and is **forced** to that inclusivity on read (a contradicting payload flag is
ignored); serializing a value whose bound contradicts the mode throws
`JsonException`. Subclass it in one line and apply with `[JsonConverter]`:

```csharp
using StrongTypes;
using System.Text.Json.Serialization;

// [start, end) — start inclusive, end exclusive, neither flag on the wire.
public sealed class HalfOpenIntervalConverter()
    : IntervalJsonConverter<FiniteInterval<int>>(IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysExclusive);

public sealed class Shift
{
    [JsonConverter(typeof(HalfOpenIntervalConverter))]
    public FiniteInterval<int> Window { get; set; }   // { "Start": 9, "End": 17 } ⇒ [9, 17)
}
```

Or add an instance to `JsonSerializerOptions.Converters` for options-wide use:
`options.Converters.Add(new IntervalJsonConverter<FiniteInterval<int>>(IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysExclusive));`.
Modes can be mixed per bound (e.g. `Stored` start, `AlwaysExclusive` end).

## EF Core

`Kalicz.StrongTypes.EfCore` offers two persistence shapes. Both re-validate on
read: a stored row violating `Start <= End` throws when materialized — the JSON
shape through its converter, the two-column shape through the validation that
`UseStrongTypes()` registers.

**Two scalar columns (the default).** Under `UseStrongTypes()`, every interval
property auto-maps as an EF Core complex type — no per-property configuration —
so each endpoint becomes its own queryable, **indexable** column, nullable
exactly when the variant's endpoint is, and endpoint access in LINQ translates
to a plain column reference:

```csharp
var hits = await db.Bookings.Where(b => b.Window.Start <= at && at <= b.Window.End).ToListAsync();
```

Only endpoint access translates. `Overlaps`, `GetOverlap`, and the
date-bridging helpers are in-memory only — in a server-side query, spell the
condition out as endpoint comparisons like the one above, using the operators
that match the property's bound convention.

Endpoint columns are named `Start` / `End`, prefixed with the property name
(`Window_Start`) when two intervals on the same entity would clash. A nullable
interval property additionally gets a shadow discriminator column, keeping a
`null` property distinct from a stored interval whose endpoints are all `NULL`
(a fully-unbounded `Interval<T>`).

**Bound inclusivity is a mapping choice** (`IntervalBoundMode`, per bound). The
default is `AlwaysInclusive`: no flag columns, and saving a value whose bound
is exclusive throws with a message pointing at the fix. Pick per property via
`HasIntervalColumns`:

- `AlwaysInclusive` / `AlwaysExclusive` — every stored value has that bound;
  no extra column. On read, the configured bound is restored. `AlwaysExclusive`
  requires `UseStrongTypes()` (its read fix-up applies the mode).
- `Stored` — adds a `StartInclusive` / `EndInclusive` `bit` column and
  round-trips each value's own flags. The flag is a plain column, so
  `Where(b => !b.Window.EndInclusive)` translates.

```csharp
modelBuilder.Entity<Shift>()
    .HasIntervalColumns(s => s.Window, endBound: IntervalBoundMode.AlwaysExclusive);   // [start, end) windows

modelBuilder.Entity<Promo>()
    .HasIntervalColumns(p => p.Window, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
```

To rename the endpoint columns — or to get this mapping without
`UseStrongTypes()` — call `HasIntervalColumns` explicitly:

```csharp
modelBuilder.Entity<Booking>()
    .HasIntervalColumns(b => b.Window, startName: "WindowStart", endName: "WindowEnd");

modelBuilder.Entity<Booking>()
    .HasIntervalColumns(b => b.MaybeWindow);   // Interval<int>? → endpoint columns + discriminator
```

To index an endpoint, add the index in a migration — EF Core cannot declare an
index over a complex-type member (`HasIndex(b => b.Window.Start)` is rejected),
but the endpoint is a plain column on the entity's table:

```csharp
migrationBuilder.CreateIndex(name: "IX_Bookings_WindowStart", table: "Bookings", column: "WindowStart");
```

**One JSON column** — opt in with `HasIntervalJsonConversion`, on the entity
builder or the property builder (the latter also handles nullable properties).
The interval round-trips through its validating converter into a single column
(`jsonb` on PostgreSQL, `nvarchar(max)` on SQL Server); bound flags ride in the
payload (written only when `false`), so no `IntervalBoundMode` is involved.
Endpoint access in LINQ translates to a server-side JSON path lookup
(`JSON_VALUE(...)` / `->>`) — still filterable and orderable, just not
index-backed:

```csharp
modelBuilder.Entity<Booking>()
    .HasIntervalJsonConversion(b => b.Window);

modelBuilder.Entity<Booking>()
    .Property(b => b.MaybeWindow)              // FiniteInterval<int>?
    .HasIntervalJsonConversion();
```

Column-mapped intervals re-validate on read only when `UseStrongTypes()` is
registered — without it, two-column reads trust the database.

## Modelling tips

Pick the variant that states the truth. A subscription that has started but
may run forever is `IntervalFrom<DateOnly>`, not `Interval<DateOnly>` with a
`Start` you have to remember is always set — the type already says so, and the
compiler stops you constructing it without a start.

```csharp
public record Subscription(
    NonEmptyString          Plan,
    IntervalFrom<DateOnly>  ActivePeriod);   // started; open-ended until cancelled
```
