## Collection and exception helpers

Utility extensions that don't fit a "strong type" bucket but round out the
library.

### `IEnumerable<T>` extensions

- `ExceptNulls()` — filter out nulls in one step:
  ```csharp
  IEnumerable<string> names = source.ExceptNulls();   // source : IEnumerable<string?>
  ```
  Works for both reference-nullable and `Nullable<T>` sources.

- `Except(params T[] items)` — exclude a known handful of elements by
  value, without building a set yourself.

- `Concat(params T[] items)` and `Concat(params IEnumerable<T>[] others)`
  — flatten a few extras into a sequence without `.Concat(new[] { ... })`:
  ```csharp
  var all = existing.Concat(1, 2, 3);
  var all = existing.Concat(list1, list2, list3);
  ```

- `Flatten()` on `IEnumerable<IEnumerable<T>>` — an alias for
  `SelectMany(x => x)` that reads better at a call site.

- `OrEmptyIfNull()` — coalesce a null collection reference to an empty
  one of the same interface (`IEnumerable<T>`, `List<T>`,
  `IReadOnlyList<T>`, `ICollection<T>`). No allocation when the input
  is non-null.

- `Partition(Func<T, bool> predicate)` — split in a single pass into two
  `IReadOnlyList<T>`:
  ```csharp
  var (passing, violating) = users.Partition(u => u.IsActive);
  ```

- `ToReadOnlyList()` / `AsReadOnlyList()` / `AsList()` — cheap view
  conversions. `AsReadOnlyList` and `AsList` are zero-alloc if the source
  already implements the target interface.

### `ReadOnlyList`

```csharp
IReadOnlyList<int> list = ReadOnlyList.Create(1, 2, 3);
IReadOnlyList<int> flat = ReadOnlyList.CreateFlat(a, b, c);   // IEnumerable<T>[]
```

Lightweight factories when you want an `IReadOnlyList<T>` and don't care
about the concrete implementation.

### `Result` partition helpers

For a collection of `Result<T, TError>`:

```csharp
var (successes, errors) = results.Partition();   // IReadOnlyList<T>, IReadOnlyList<TError>

// Side-effect fold
results.PartitionMatch(
    successes: ok   => Save(ok),
    errors:    bad  => Log(bad));

// Projection fold — returns R[]
R[] merged = results.PartitionMatch(
    successes: ok   => Summarise(ok),
    errors:    bad  => Describe(bad));
```

### `Exception` aggregate

Turn a collection of exceptions into a single `AggregateException`,
returning null when the source is empty (or the single exception when
there's only one):

```csharp
Exception? agg = exceptions.Aggregate();                 // null on empty
Exception? agg = list.Aggregate();                       // IReadOnlyList<Exception>
Exception  agg = nonEmptyList.Aggregate();               // INonEmptyEnumerable<Exception>
```

This is what `Result.Aggregate(...)` uses under the hood when `TError`
is `Exception`.

### Boolean helper

```csharp
bool ok = condition.Implies(consequence);           // !condition || consequence
bool ok = condition.Implies(() => ExpensiveCheck()); // short-circuiting
```

Reads cleanly in guard clauses (`Debug.Assert(isLoaded.Implies(size > 0))`).
