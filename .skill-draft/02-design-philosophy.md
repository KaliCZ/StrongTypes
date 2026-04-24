## Design philosophy — picking the right wrapper

This is the most important section. Most misuses of StrongTypes come from
reaching for `Maybe<T>` or `Result<T, TError>` when a plain `T?` would do.
Read the decision tree before writing a new DTO field or service signature.

### Decision tree for "optional / nullable" fields

1. **Can the field be absent, but never be explicitly cleared?** → use `T?`.
   Example: an `OrderUpdate` DTO's `Price` property. Null means "don't touch
   the price". You can't "remove" a price, so there are only two states,
   and `decimal?` captures them both.

2. **Can the field be absent *and* be explicitly cleared to null?** → use
   `Maybe<T>?`. This is the HTTP `PATCH` three-state case:
   - `null` → client did not send the property; leave the entity alone.
   - `Maybe<T>.None` (`{ "Value": null }` on the wire) → client asked to
     clear the field.
   - `Maybe.Some(x)` (`{ "Value": x }`) → client asked to set the field.

3. **Is the field always present and meaningful?** → the bare strong type
   (`NonEmptyString`, `Positive<int>`, …). No nullable wrapping.

```csharp
public record OrderUpdate(
    decimal?         Price,          // optional update; cannot be cleared
    Maybe<string>?   Nickname,       // three-state: skip / clear / set
    NonEmptyString   OrderCode       // always required
);
```

### Decision tree for validation / parsing results

1. **Is this a single-reason validation where the caller turns the failure
   straight into an HTTP 400 or an exception?** → return `T?` from
   `TryCreate` / `As…`. Caller unwraps with `is not { } v`:

   ```csharp
   public IActionResult CreateUser(string? nameInput)
   {
       if (nameInput.AsNonEmpty() is not { } name)
           return BadRequest("name must not be empty");

       // 'name' is NonEmptyString from here on — the whole service
       // call tree now has a typed, validated name.
       return Ok(_service.Create(name));
   }
   ```

   Every downstream call sees `NonEmptyString`, not `string`. No `Result`
   is needed because the caller already knows *why* the parse failed —
   the validation rule is baked into the wrapper's name.

2. **Does the caller need to distinguish between multiple failure
   reasons, translate them to user-facing codes, or aggregate them with
   other failures?** → return `Result<T, TError>`. `TError` is normally an
   enum, occasionally a string if you don't need localisation.

   ```csharp
   public enum OrderError { PaymentFailed, OutOfStock, InvalidAddress }

   public Result<Order, OrderError> CreateOrder(OrderData data) { ... }
   ```

3. **Does an exception fit the failure better?** → use `Create` /
   `ToNonEmpty` / `ToPositive`, which throw `ArgumentException`. Don't
   invent a `Result<Order, Exception>` to smuggle an exception through.

### Summary rule

> **`T?` is the default.** Reach for `Maybe<T>` only when you need the
> three-state distinction (PATCH-style). Reach for `Result<T, TError>`
> only when the caller needs the error *reason*, not just the fact of
> failure. Everything else is a primitive or a strong wrapper.

### Why not just return `Result` from every validator?

Because the caller already has the reason:

- The rule is encoded in the method name (`AsNonEmpty` means "empty or
  whitespace failed").
- The caller that consumes the failure usually wants to turn it into a
  400 response, a `ModelState` error, or a thrown exception — and they
  already know which. Returning a `Result<T, SomeEnum>` just to put
  *one* possible error in the enum is noise.
- `Result` shines when you want to **aggregate** multiple validation
  outcomes and report them together. That's `Result.Aggregate(...)`,
  which is a deliberate top-level use case, not a default pattern.

### Result flow in practice

Services return `Result<T, TError>`. Controllers *consume* those results
and turn them into HTTP responses, but controllers themselves rarely
need to *construct* a `Result` — if a controller-level check fails, it
just returns a `BadRequest(...)` directly.

```csharp
// Controller: consume service Results; do local validation with T?.
[HttpPost]
public async Task<IActionResult> Create(CreateRequest request)
{
    if (request.Name.AsNonEmpty() is not { } name)
        return BadRequest("name required");

    Result<Order, OrderError> result = await _orders.Create(name, request.Items);
    if (result.Error is { } e)
        return Problem(MapError(e));

    return Created("...", result.Success);
}
```
