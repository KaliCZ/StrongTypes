## Implicit operators — prefer them over explicit factories

Every wrapper in the library ships implicit operators that let you drop a
plain value into a wrapper slot without naming the wrapper type. Use them.
They are shorter, they help type inference, and they avoid spelling out
generic parameters twice.

### Reference table

| From              | To                     | Operator   | Example                                                       |
| ----------------- | ---------------------- | ---------- | ------------------------------------------------------------- |
| `NonEmptyString`  | `string`               | implicit   | `string s = name;`                                            |
| `string`          | `NonEmptyString`       | explicit   | `(NonEmptyString)s` — throws if invalid; prefer `AsNonEmpty()`|
| `Positive<T>`     | `T`                    | implicit   | `int i = positive;` (all numeric wrappers)                    |
| `T`               | `Positive<T>`          | explicit   | `(Positive<int>)42` — throws; prefer `AsPositive()`           |
| `Digit`           | `byte`, `int`          | implicit   | `int d = digit;`                                              |
| `T`               | `Maybe<T>`             | implicit   | `Maybe<int> m = 42;`                                          |
| `Maybe.None`      | any `Maybe<T>`         | implicit   | `Maybe<int> m = Maybe.None;`                                  |
| `T`               | `Result<T, TError>`    | implicit   | `return value;` (the success branch)                          |
| `TError`          | `Result<T, TError>`    | implicit   | `return OrderError.PaymentFailed;`                            |
| `T`               | `Result<T>`            | implicit   | `return value;`                                               |
| `Exception`       | `Result<T>`            | implicit   | `return new InvalidOperationException(...);`                  |

### Return sites — just `return`

Do **not** write `Result<T, TError>.Success(value)` or
`Result.Success<T, TError>(value)` when the method already declares the
return type. Type inference picks the right branch off the implicit
operators:

```csharp
// Correct — implicit operators do the wrapping.
public Result<Order, OrderError> Place(OrderData data)
{
    if (data.Items.Count == 0)
        return OrderError.EmptyCart;                // → error branch

    return new Order(data);                         // → success branch
}

// Ternary also works.
public Result<int, string> Parse(string s)
    => int.TryParse(s, out var n) ? n : "not a number";
```

The explicit factories (`Result.Success<T, TError>(value)` /
`Result.Error<T, TError>(error)`) are a fallback for the occasional spot
where inference can't pick a branch — for example a ternary inside a
`var`-typed local where `T` and `TError` happen to be the same type. Reach
for them only when the compiler actually complains.

### `Maybe<T>` follows the same rule

```csharp
// Preferred
Maybe<int> some = 42;
Maybe<int> none = Maybe.None;

// Usually unnecessary
Maybe<int> some = Maybe.Some(42);
Maybe<int> some = Maybe<int>.Some(42);
```

`Maybe.Some(value)` is still useful when the compiler can't infer `T`
— for example inside a `var`-typed collection expression where every
element is `Maybe.None`.

### Equality and comparison work through implicits too

Every wrapper implements `IEquatable<TUnderlying>` and `IComparable<TUnderlying>`
on top of `IEquatable<Self>` / `IComparable<Self>`. You do not need to
unwrap before comparing:

```csharp
NonEmptyString.Create("alice") == "alice";   // true
2 == Positive<int>.Create(2);                // true
Positive<int>.Create(4) > 2;                 // true
Maybe.Some(3) == 3;                          // true
Maybe<int>.None < 0;                         // true (None sorts before any value)
```

### When to keep the wrapper visible

One common mistake is unwrapping too eagerly. Once you have a
`NonEmptyString`, pass it around as `NonEmptyString` — don't call `.Value`
just because a helper signature takes `string`. If the helper should enforce
non-empty input, change its signature. If it really takes any string, the
implicit conversion handles it at the call site.

```csharp
// Good — downstream stays typed.
public void Greet(NonEmptyString name) => Console.WriteLine($"hi, {name}");

// Wrong — unwraps for no reason.
public void Greet(NonEmptyString name) => Console.WriteLine($"hi, {name.Value}");
```
