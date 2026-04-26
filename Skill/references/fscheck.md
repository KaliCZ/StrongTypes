# FsCheck — `Kalicz.StrongTypes.FsCheck`

## When to reach for property tests

**Default to property tests whenever you are testing an invariant over an
input space.** That covers most of what you'd otherwise write as a
`[Theory]` with a handful of `[InlineData]` rows: validation rules, parse
round-trips, equality / comparison contracts, ordering laws, "this output
is always sorted / always non-empty / always within bounds" checks.

A property test states the rule once and lets FsCheck try hundreds of
inputs — including the awkward ones you wouldn't think to write by hand
(zeros, max values, surrogate-pair strings, empty / single-element
sequences). When the rule breaks, FsCheck shrinks the failure to a
minimal counterexample.

Fall back to `[Fact]` or `[Theory] + [InlineData]` only when:

- The input space genuinely doesn't generate cleanly (e.g. you really
  do mean "verify exactly these three rows" — a worked-example test).
- The body asserts a side effect that doesn't generalise ("the factory
  was invoked exactly once").
- A custom generator would be more code than the test is worth.

## Registration

One attribute on the test class points FsCheck at the project's
`Generators` class:

```csharp
using FsCheck.Xunit;
using StrongTypes.FsCheck;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MyTests
{
    [Property]
    public void NonEmptyString_is_never_whitespace(NonEmptyString value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value));
    }

    [Property]
    public void Positive_stays_positive(Positive<int> value)
    {
        Assert.True(value.Value > 0);
    }
}
```

You don't name individual arbitraries — declare the strong type as a
parameter and FsCheck resolves it by type from the registered class.

## Before writing a new generator: check what exists

Adding a generator per test, or per feature, fragments the test project.
The convention is **one shared `Generators` class per test project** that
re-exports both the library's arbitraries and any project-specific ones.

When you need a generator for some type `T`, in order:

1. **Check the project's local `Generators` class first.** It usually
   lives at `<TestProject>/Generators.cs`. If `T` already has an
   arbitrary there, register that class and you're done.

2. **Check what `Kalicz.StrongTypes.FsCheck` ships.** Every scalar
   strong type comes with three arbitraries — the bare type, its
   nullable form (~5% null), and a `Maybe<T>` form (~5% None). Names
   follow `<Shape><TypeName>` (e.g. `MaybeNonEmptyString`,
   `NullablePositiveInt`). Numeric coverage targets `int`; collection
   coverage is `NonEmptyEnumerableInt`. Plain
   `Maybe<bool|int|long|double|char|string|Guid>` arbitraries ship too.
   List the shipped types with the IDE / decompiler / a quick `grep` of
   the FsCheck assembly's public surface — don't guess names.

3. **Only then write a new arbitrary.** Add it to the same `Generators`
   class — never a per-feature generator file. That keeps a single
   attribute (`[Properties(Arbitrary = new[] { typeof(Generators) })]`)
   sufficient for every test class.

When a custom generator is non-trivial (weighted branches, conditional
shrinking, composite types), pair it with a one-off `[Fact]` that samples
it a few hundred times and asserts every partition branch appears. That
catches a regression in the generator before it silently masks missing
coverage.

## Weighting branches

Use `Gen.Frequency` when one branch is the common path and the other
needs occasional coverage. A typical populated-vs-null split is ~90 / 10
— rare enough that the populated path dominates, common enough that
shrinking still finds null-related bugs:

```csharp
public static Arbitrary<string?> NullableTextArbitrary() =>
    Gen.Frequency(
        (9, Arb.Default.String().Generator),
        (1, Gen.Constant<string?>(null))
    ).ToArbitrary();
```
