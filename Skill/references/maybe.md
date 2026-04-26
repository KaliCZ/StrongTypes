# `Maybe<T>`

A `readonly struct` that is either `Some(T)` or `None`. Constrained to
`T : notnull` — `Maybe<int?>` and `Maybe<string?>` are disallowed because
they would collapse the `None` and `Some(null)` cases.

> **Before reaching for `Maybe<T>`, re-read the design-philosophy section
> in SKILL.md.** Most optional fields should be plain `T?`. `Maybe<T>`
> exists for the three-state case — wherever a field or parameter needs
> to mean "skip", "clear", *and* "set" at the same time. HTTP `PATCH`
> DTOs are the most visible example, but the same pattern applies to
> plain in-process update methods (service calls, builders, message
> payloads) with no HTTP involved. It is also a total replacement for
> the throwing `First` / `Single` / `Max` LINQ calls.

## Construction — prefer implicit operators

```csharp
Maybe<int>    some = 42;              // implicit operator T → Maybe<T>
Maybe<int>    none = Maybe.None;      // untyped None, binds to the slot type

// Explicit only when inference can't help.
Maybe<int>    some = Maybe.Some(42);          // T inferred
Maybe<int>    some = Maybe<int>.Some(42);     // fully explicit

// From a nullable
Maybe<int>    m1 = nullableInt.ToMaybe();     // Some when HasValue, None otherwise
Maybe<string> m2 = nullableString.ToMaybe();  // Some when non-null, None otherwise
```

`Maybe.None` is a separate `MaybeNone` value with an implicit conversion
to any closed `Maybe<T>`, so collection expressions can mix literals and
None markers cleanly:

```csharp
Maybe<int>[] xs = [1, 2, Maybe.None, 4];
IEnumerable<int> values = xs.Values();   // [1, 2, 4]
```

## Inspection — `is { } v` on `.Value`

The idiomatic unwrap uses the `Value` extension property. For value types
it returns `Nullable<T>`; for reference types it returns `T?`. Either way
the `is { } v` pattern unwraps to the underlying `T`:

```csharp
if (maybe.Value is { } v)
{
    // v is int (not int?) / string (not string?)
}
```

Other inspection surface:

- `IsSome`, `IsNone`, `HasValue` — all `bool`.
- `Match(Func<T, R> ifSome, Func<R> ifNone)` — fold to a value.
- `Match(Action<T>? ifSome = null, Action? ifNone = null)` — side-effect form.

## Composition — `Map`, `FlatMap`, `Where`

```csharp
Maybe<int>     doubled = Maybe.Some(3).Map(x => x * 2);          // Some(6)
Maybe<int>     gone    = Maybe<int>.None.Map(x => x * 2);        // None

Maybe<int>     parsed  = Maybe.Some("42").FlatMap(Parse);        // Some(42)
Maybe<int>     filtered = Maybe.Some(4).Where(x => x % 2 == 0);  // Some(4)

// LINQ query syntax works via Select / SelectMany.
var sum = from a in Maybe<int>.Some(2)
          from b in Maybe<int>.Some(3)
          select a + b;                                          // Some(5)
```

Async counterparts: `MapAsync`, `FlatMapAsync`, `MatchAsync`.

## Collection helpers — `SafeX` and `Values`

These are drop-in replacements for throwing LINQ operators. Use them when
an empty sequence (or no match) is a legitimate result, not an error:

```csharp
Maybe<User> m = users.SafeFirst(u => u.Age > 18);
Maybe<User> m = users.SafeSingle(u => u.Id == target);
Maybe<User> m = users.SafeLast();
Maybe<int>  hi = scores.SafeMax();
Maybe<int>  lo = scores.SafeMin();
Maybe<Score> worst = results.SafeMin(r => r.Value);

// Drop Nones in one shot.
IEnumerable<int> values = maybes.Values();
```

(When the source sequence is already non-empty — i.e. a
`NonEmptyEnumerable<T>` — use the total `Max`, `Min`, `Last` that return
`T` directly. `SafeX` is for the general `IEnumerable<T>` case.)

## JSON

`Maybe<T>` serialises as `{ "Value": x }` for `Some` and either
`{ "Value": null }` or `{}` for `None`. The converter is on the type, so
no registration is needed. The same `[JsonConverter]` attribute is also
what makes the `Maybe<T>?` update pattern work over the wire — a missing
property deserializes as `null` on the outer nullable; a `{}` or
`{ "Value": null }` deserializes as `Maybe<T>.None`; anything else
becomes `Maybe.Some(value)`.

## Three-state updates — the canonical case

The pattern applies wherever an update needs to distinguish "skip",
"clear", and "set" — HTTP `PATCH` or pure in-process code alike.

**In-process service call — no HTTP involved.** An `ApplyChanges` method
whose caller can leave a field alone, clear it, or set a new value:

```csharp
public record ProfileChanges(
    Maybe<string>? Nickname,        // null = skip; None = clear; Some(x) = set
    Maybe<DateOnly>? Birthday       // same three states
);

public sealed class ProfileService(IProfileRepository repo)
{
    public async Task ApplyChanges(Guid userId, ProfileChanges changes)
    {
        var profile = await repo.LoadAsync(userId);

        if (changes.Nickname is { } nick)
            profile.Nickname = nick.Value;       // nick.Value: string?
        if (changes.Birthday is { } bday)
            profile.Birthday = bday.Value;

        await repo.SaveAsync(profile);
    }
}
```

The caller of `ApplyChanges` may be a controller, a background job, a
message consumer, another service — none of that matters. The three
states exist purely in the domain.

**HTTP PATCH — the same pattern once more, across the wire.** The JSON
converter takes care of the mapping; the handler body is identical in
shape:

```csharp
public record PatchRequest(
    Maybe<string>? Nickname   // null = skip; None = clear; Some(x) = set
);

[HttpPatch("{id:guid}")]
public async Task<IActionResult> Patch(Guid id, PatchRequest request)
{
    var entity = await _db.FindAsync<Entity>(id);
    if (entity is null) return NotFound();

    if (request.Nickname is { } nick)
        entity.Nickname = nick.Value;   // Value: string? — null when None, value when Some

    await _db.SaveChangesAsync();
    return Ok();
}
```

Note the double unwrap in both examples: `request.Nickname is { } nick`
tells you the caller sent *something*; `nick.Value` is then the intended
new value (possibly null for the "clear" intent).
