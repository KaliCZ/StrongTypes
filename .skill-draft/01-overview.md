## What StrongTypes is

`Kalicz.StrongTypes` is a small C# library of focused value wrappers and
algebraic types that push invariants into the type system. Core ideas:

- **Make invalid states unrepresentable at the boundary.** Once a string
  becomes a `NonEmptyString`, no downstream code has to re-check that it is
  non-null / non-whitespace. Same for `Positive<int>`, `NonEmptyEnumerable<T>`,
  etc.
- **Fail at the edge.** All wrapped types ship with `System.Text.Json`
  converters, so bad input surfaces as a `JsonException` before your endpoint
  method runs — not deep in a service call.
- **Interoperate with plain C#.** These types implement `IEquatable`,
  `IComparable`, expose implicit conversions where safe, and — critically —
  are designed to sit next to ordinary `string?` / `int` / `T?` without
  demanding a wholesale functional rewrite.

StrongTypes is *not* trying to be an F#-style algebraic language on top of
C#. `Maybe<T>` and `Result<T, TError>` exist for the few places where plain
C# genuinely can't express what you want. The rest of your code should still
look like idiomatic modern C# with nullable reference types.

## Packages

| Package                       | What it gives you                                                                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `Kalicz.StrongTypes`          | The core types (`NonEmptyString`, numeric wrappers, `NonEmptyEnumerable<T>`, `Maybe<T>`, `Result<T, TError>`, …).  |
| `Kalicz.StrongTypes.EfCore`   | EF Core value converters + a `.Unwrap()` LINQ translator so strong types sit directly on entity properties.        |
| `Kalicz.StrongTypes.FsCheck`  | FsCheck `Arbitrary<T>` generators — one `[Properties(Arbitrary = new[] { typeof(Generators) })]` and you're done.  |

Install the core package first. Add EfCore / FsCheck only when you hit those
stacks.

## When to reach for StrongTypes

Reach for it when:

- A primitive parameter has an invariant ("must be non-empty", "must be
  positive", "list must have at least one item"). Use the matching wrapper
  at the API / DTO boundary.
- You want JSON to reject bad input for you at deserialization time.
- You have an HTTP `PATCH`-style three-state field (skip / clear / set).
  That is the canonical `Maybe<T>?` case — see the design-philosophy section.
- A method's failure mode needs to be visible at the call site, translated
  to a user-facing error, or aggregated with other failures. Use
  `Result<T, TError>` — usually `TError` is an enum.

Do **not** reach for it when:

- Plain nullable (`string?`, `int?`, `T?`) already captures the state. A
  DTO field that means "no value" is `T?`, not `Maybe<T>`.
- You'd be writing `Result<T, ErrorEnum>` with a single error value and no
  caller that consumes it differently from an exception. A failing parse
  on user input that immediately becomes a 400 is cleaner as
  `input.AsNonEmpty() is not { } name` and returning early — no `Result`
  needed.
