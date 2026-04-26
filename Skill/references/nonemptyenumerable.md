# `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>`

`NonEmptyEnumerable<T>` is a sealed reference type that wraps a sequence
guaranteed to have at least one element. `INonEmptyEnumerable<out T>` is
a covariant interface for when you need to assign
`NonEmptyEnumerable<Dog>` into `INonEmptyEnumerable<Animal>`.

## Construction

```csharp
// Params / collection expressions
var list = NonEmptyEnumerable.Create(1, 2, 3);
NonEmptyEnumerable<int> list = [1, 2, 3];      // empty [] throws at construction

// Runtime sequences — use CreateRange (Create is taken by the collection-expression form)
NonEmptyEnumerable<int>  thrown    = NonEmptyEnumerable.CreateRange(source);
NonEmptyEnumerable<int>? nullable  = NonEmptyEnumerable.TryCreateRange(source);

// Extensions on IEnumerable<T>? — same semantics, nicer syntax
NonEmptyEnumerable<int>? maybe     = values.AsNonEmpty();   // null if empty/null
NonEmptyEnumerable<int>  required  = values.ToNonEmpty();   // throws if empty/null
```

Throws / returns null identically for null input and for empty input.

## Guaranteed shape

```csharp
T                head  = list.Head;    // always defined
IReadOnlyList<T> tail  = list.Tail;    // everything after Head
int              count = list.Count;   // always >= 1
```

`NonEmptyEnumerable<T>` implements `IReadOnlyList<T>` and has a struct
enumerator — no allocations on `foreach`.

## Invariant-preserving LINQ

These operations keep the non-empty guarantee and return
`NonEmptyEnumerable<TResult>`:

- `Select(...)`, including the indexed overload.
- `SelectMany(...)` *when the inner result is itself non-empty*.
- `Distinct()`, `Distinct(IEqualityComparer<T>)`.
- `Concat(params T[])`, `Concat(IEnumerable<T>)`, both on
  `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>`.
- `Flatten()` on `INonEmptyEnumerable<INonEmptyEnumerable<T>>`.
- `T head.Concat(params IEnumerable<T>[] tails)` — builds a
  `NonEmptyEnumerable<T>` from a head + tails.
- `Prepend(T)`, `Append(T)`.
- `Reverse()`.
- `Take(Positive<int> count)` — the count wrapper enforces `>= 1` so
  the result cannot be empty. The `int` overload of `Take` is also
  available but prefer the `Positive<int>` one.

Operations that can produce an empty sequence (`Where`, `Skip`, `TakeWhile`,
`GroupBy`, …) fall through to standard LINQ and return `IEnumerable<T>`.
Re-wrap with `.AsNonEmpty()` / `.ToNonEmpty()` if you need the guarantee
back.

## Total aggregates

Because the sequence is known non-empty, aggregates that usually throw
`InvalidOperationException` on empty input are total here. For value
types, they return `T` directly, not `T?`:

```csharp
int max   = list.Max();
int min   = list.Min();
int last  = list.Last();
int sum   = list.Aggregate((a, b) => a + b);
int avg   = list.Average();             // where T : INumber<T>
T   maxBy = list.MaxBy(x => x.Key);
T   minBy = list.MinBy(x => x.Key);
```

All of these have overloads on both `NonEmptyEnumerable<T>` and
`INonEmptyEnumerable<T>`, so chains on either receiver type work.

## Covariance

```csharp
NonEmptyEnumerable<Dog>     dogs    = [new Dog()];
INonEmptyEnumerable<Animal> animals = dogs;   // ok — `out T` on the interface
```

Use `INonEmptyEnumerable<T>` in method parameters when you want callers
to be free to pass the more-derived form.

## JSON

Serialises as a JSON array. An empty JSON array (`[]`) is rejected with
`JsonException`. `NonEmptyEnumerable<T?>` accepts JSON `null` elements —
`[1, null, 3]` round-trips into `NonEmptyEnumerable<int?>` faithfully.

> ⚠ The usual C# caveat about `null` inside a reference-typed collection
> still applies: a JSON `[null]` will deserialize into
> `NonEmptyEnumerable<string>` even though `string` isn't annotated
> nullable. That's the same hole `List<string>` has — the library doesn't
> plug it.

## When to use it

- Method parameters where "must have at least one" is the real
  contract: `Notify(INonEmptyEnumerable<UserId> recipients)`, aggregate
  operations over a batch, etc. No defensive empty check inside the body.
- Return types from operations that always produce at least one element
  (a decomposition, a fan-out, a header + body shape).

Don't wrap lists where empty is a perfectly valid state — that just
pushes a `.ToNonEmpty()` or an awkward `if` to every caller.
