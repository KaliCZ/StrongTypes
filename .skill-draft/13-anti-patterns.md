## Anti-patterns — common misuses to avoid

A cheat sheet of mistakes that show up when people adopt the library
before they've internalised the design philosophy.

### 1. Using `Maybe<T>` for a plain optional field

```csharp
// Wrong — no three-state intent; the field just might not exist.
public record Profile(Maybe<string> Bio);

// Right — T? captures "might be absent" already.
public record Profile(string? Bio);
```

`Maybe<T>` is for the **three-state** case (skip / clear / set), typically
HTTP PATCH. If there's no "clear" intent, `T?` is enough.

### 2. Using `Maybe<T>` for update DTOs that can't remove the value

```csharp
// Wrong — Price cannot be "removed". Only "skipped" or "set".
public record OrderUpdate(Maybe<decimal> Price);

// Right — null means "don't update", decimal value means "set".
public record OrderUpdate(decimal? Price);
```

The rule: reach for `Maybe<T>?` only when the field supports all three of
"don't touch", "clear to null", and "set to value". If "clear to null"
is meaningless, plain `T?` is correct.

### 3. Using `Result<T, E>` for single-reason validations

```csharp
// Wrong — the caller doesn't need the reason; "name must not be empty"
// is already implied by the NonEmptyString name.
public Result<NonEmptyString, string> ParseName(string? input) =>
    input.AsNonEmpty().ToResult("name required");

// Right — let the caller decide how to react to null.
public NonEmptyString? ParseName(string? input) => input.AsNonEmpty();

// Call site
if (ParseName(request.Name) is not { } name)
    return BadRequest("name required");
```

`Result<T, TError>` earns its keep when the caller needs to distinguish
*which* failure occurred — typically an enum with multiple cases, often
aggregated across several inputs. A single-reason parse is cleaner as
`T?` + pattern matching.

### 4. Spelling out explicit factories when implicit operators suffice

```csharp
// Wrong — unnecessary ceremony.
return Result<Order, OrderError>.Success(order);
return Result.Error<Order, OrderError>(OrderError.OutOfStock);

// Right — return-type inference picks the branch.
return order;
return OrderError.OutOfStock;

// Same for Maybe<T>
Maybe<int> x = Maybe<int>.Some(42);   // unnecessary
Maybe<int> x = 42;                     // idiomatic
```

Explicit factories (`Result.Success<T, TError>`, `Maybe<T>.Some`) are for
the rare inference-collision case (e.g. `T == TError`). Default is `return
value;`.

### 5. Unwrapping a wrapper the moment you can

```csharp
// Wrong — loses the invariant on the very next line.
public void Greet(NonEmptyString name)
    => _downstream.Greet(name.Value);

// Right — implicit conversion already exists. Keep NonEmptyString flowing.
public void Greet(NonEmptyString name)
    => _downstream.Greet(name);
```

If the downstream signature is `string`, the implicit conversion handles
the interop. If the downstream should enforce non-empty, change its
signature. Reaching for `.Value` should be the exception, not the norm.

### 6. Constructing wrappers through the throwing factory in a controller

```csharp
// Wrong — throws into ASP.NET's exception pipeline for user input.
var name = NonEmptyString.Create(request.Name);

// Right — treat user input with the nullable-returning factory.
if (request.Name.AsNonEmpty() is not { } name)
    return BadRequest("name required");
```

`Create` / `ToX` is for *internal* code where invalid input is a bug and
throwing is the correct response. `TryCreate` / `AsX` is for *external*
input where invalid means "reply with a 400".

### 7. Writing your own JSON converter for a wrapper

Don't. Every wrapper in the library already ships a converter. If you
write a custom one, you likely have a bug (e.g. your converter doesn't
validate) or you're fighting a configuration issue elsewhere.

### 8. Using `NonEmptyEnumerable<T>` for "probably not empty, usually"

```csharp
// Wrong — tags is naturally allowed to be empty.
public record Article(string Title, NonEmptyEnumerable<string> Tags);

// Right — an empty tag list is valid.
public record Article(string Title, IReadOnlyList<string> Tags);
```

Use `NonEmptyEnumerable<T>` only where "zero elements" really is an
error — batch recipients, decomposed paths, aggregate inputs. Otherwise
every caller pays a `.ToNonEmpty()` tax.

### 9. Forgetting to call `.Unwrap()` in EF LINQ

```csharp
// Doesn't translate — EF can't call string.StartsWith on a NonEmptyString in SQL.
db.Users.Where(u => u.Name.StartsWith("ali"))

// Right — .Unwrap() rewrites to a bare column reference for SQL.
db.Users.Where(u => u.Name.Unwrap().StartsWith("ali"))
```

Equality / ordering / null-checks on the wrapper work directly. Anything
using the underlying type's operators (`Contains`, arithmetic,
`EF.Functions.*`) needs `.Unwrap()`.
