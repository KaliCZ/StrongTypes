## `Result<T, TError>`

Either a success carrying a `T` or an error carrying a `TError`.
`Result<T>` is the alias for `Result<T, Exception>` — use it when you just
want to represent "might throw" without picking a specific exception type.

Use `Result` when the caller needs to distinguish *why* something failed,
aggregate multiple failures together, or translate a domain failure into a
user-facing error. Otherwise use `T?` (see design-philosophy section).

### Construction — `return value;`

Implicit operators convert both a `T` and a `TError` into the appropriate
`Result<T, TError>` branch. Once the method's return type is declared,
`return someT;` or `return someError;` is enough — **do not** write
`Result<T, TError>.Success(x)` or `Result.Error<T, TError>(e)`. Inference
handles it.

```csharp
public Result<int, string> Parse(string s)
    => int.TryParse(s, out var n) ? n : "not a number";

public Result<Order, OrderError> Place(OrderData data)
{
    if (data.Items.Count == 0)
        return OrderError.EmptyCart;       // implicit → error branch

    return new Order(data);                // implicit → success branch
}
```

Explicit factories exist for the rare case where inference can't pick a
branch (usually when `T` and `TError` are the same type):

```csharp
var ok  = Result.Success<int, string>(42);
var err = Result.Error<int, string>("bad");
```

For `Result<T>` (the `Exception`-flavoured alias), the implicit operators
convert from `T` and from any `Exception`:

```csharp
public Result<string> Read(string path) => File.ReadAllText(path);   // success

public Result<string> ReadOrFail(string path)
    => !File.Exists(path)
        ? new FileNotFoundException(path)   // implicit → error branch
        : File.ReadAllText(path);
```

### Access

```csharp
Result<int, string> r = Parse(input);

if (r.Success is { } value) { /* value is int */ }
if (r.Error   is { } msg)   { /* msg is string */ }

bool ok = r.IsSuccess;
bool no = r.IsError;
```

`Success` / `Error` are extension properties. They return
`Nullable<T>` or `T?` so `is { } v` unwraps to the bare type.

### Fold — `Match`

```csharp
string message = r.Match(
    success: x => $"got {x}",
    error:   e => $"oops: {e}");

await r.MatchAsync(
    success: async x => await logger.LogAsync("ok", x),
    error:   async e => await logger.LogAsync("bad", e));
```

`Match` exists because C# does not yet let you switch on `Result<T, TError>`
by branch.

### Transform — `Map`, `MapError`, `FlatMap`

```csharp
// Success side — Map
Result<int, string> doubled = r.Map(x => x * 2);

// Error side — MapError
Result<int, ApiError> translated = r.MapError(msg => ApiError.Parse(msg));

// Both sides in one pass
Result<int, ApiError> both = r.Map(x => x * 2, msg => ApiError.Parse(msg));

// FlatMap — chain an operation that itself returns a Result.
Result<int, string> positive = r.FlatMap<int>(x => x > 0 ? x : "must be positive");
```

All of the above have `MapAsync`, `MapErrorAsync`, `FlatMapAsync`
counterparts.

### From nullable

When a nullable validation is the *source* of a `Result`:

```csharp
// Result<T, TError> variants (two shapes each: value and factory)
Result<NonEmptyString, string> a = name.AsNonEmpty().ToResult("name required");
Result<NonEmptyString, string> b = name.AsNonEmpty().ToResult(() => BuildMessage());

// Result<T> (Exception-flavoured)
Result<NonEmptyString> c = name.AsNonEmpty().ToResult();                        // throws default Exception on null
Result<NonEmptyString> d = name.AsNonEmpty().ToResult(new ArgumentException()); // custom exception
Result<NonEmptyString> e = name.AsNonEmpty().ToResult(() => new Ex("..."));     // factory
```

Both the value-type and reference-type overloads are present, so you can
call `ToResult` on any `T?` or `Nullable<T>`.

### Aggregate multiple validations

`Result.Aggregate` collects *all* errors, not just the first. Tuple-style
overloads up to 8 inputs, plus an `IEnumerable` overload for dynamic lists:

```csharp
record User(NonEmptyString Name, Positive<int> Age);

Result<User, string> ParseUser(string? nameInput, int ageInput)
{
    Result<NonEmptyString, string> name = nameInput.AsNonEmpty().ToResult("name must not be empty");
    Result<Positive<int>, string>  age  = ageInput.AsPositive().ToResult("age must be positive");

    return Result.Aggregate(name, age,
        (n, a) => new User(n, a),
        errors => string.Join("; ", errors));
}

// Pass the raw error list through if you don't want to merge
Result<User, string[]> u = Result.Aggregate(name, age, (n, a) => new User(n, a));

// Dynamic count
Result<Positive<int>[], string> parsed = Result.Aggregate(
    inputs.Select(i => i.AsPositive().ToResult(i)),
    invalid => $"not positive: [{string.Join(", ", invalid)}]");
```

### Catch exceptions into a Result

```csharp
Result<string>                  r = Result.Catch(() => File.ReadAllText(path));
Result<int, FormatException>    r = Result.Catch<int, FormatException>(() => int.Parse(input));

// Async
Result<string> r = await Result.CatchAsync(() => File.ReadAllTextAsync(path));
```

`OperationCanceledException` (and `TaskCanceledException`) are **not**
captured by default — cancellation unwinds normally. Opt in with
`propagateCancellation: false` if you want cancellation to surface as a
`Result` error.

### Escape hatches

```csharp
T value1 = r.ThrowIfError();                            // throws the TError directly (if it's an Exception)
T value2 = r.ThrowIfError(e => new DomainException(e)); // wrap into an exception of your choice
T value3 = aggregated.ThrowIfError();                   // for Result<T, IReadOnlyList<Exception>>
Result<T, TError> flat = nested.Flatten();              // Result<Result<T, E>, E> → Result<T, E>
```

### Flow-of-control

- Controllers **consume** `Result`s from services and convert them into
  HTTP responses (`BadRequest`, `Problem`, `Ok`, …) — they rarely
  construct a `Result` themselves.
- Services **return** `Result<T, TError>` where `TError` is a domain
  enum. The calling controller maps the enum to a user-facing code.
- Pure-C# validation in a controller (e.g. `input.AsNonEmpty() is not { }
  name`) does **not** need a `Result`.

### JSON

`Result<T, TError>` has **no** JSON converter — deliberately. Don't
serialise it. If you need to ship a result over the wire, translate it
into a dedicated response DTO first.
