# FsCheck — `Kalicz.StrongTypes.FsCheck`

One attribute on the test class:

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

## What ships

Every scalar strong type ships three shapes — the bare type, its nullable
form (~5% null), and a `Maybe<T>` form (~5% None):

| Type                | `T`              | `T?`                       | `Maybe<T>`              |
| ------------------- | ---------------- | -------------------------- | ----------------------- |
| `NonEmptyString`    | `NonEmptyString` | `NullableNonEmptyString`   | `MaybeNonEmptyString`   |
| `Digit`             | `Digit`          | `NullableDigit`            | `MaybeDigit`            |
| `Positive<int>`     | `PositiveInt`    | `NullablePositiveInt`      | `MaybePositiveInt`      |
| `Negative<int>`     | `NegativeInt`    | `NullableNegativeInt`      | `MaybeNegativeInt`      |
| `NonNegative<int>`  | `NonNegativeInt` | `NullableNonNegativeInt`   | `MaybeNonNegativeInt`   |
| `NonPositive<int>`  | `NonPositiveInt` | `NullableNonPositiveInt`   | `MaybeNonPositiveInt`   |

Also bundled: `NonEmptyEnumerableInt`, and `MaybeBool` / `MaybeInt` /
`MaybeLong` / `MaybeDouble` / `MaybeChar` / `MaybeString` / `MaybeGuid`
(all ~5% None).

## Inside a single test project

Even without the FsCheck package, keep shared arbitraries on a single
`Generators` class (convention in this repo lives at
`src/StrongTypes.Tests/Generators.cs`). One attribute per test class
(`[Properties(Arbitrary = new[] { typeof(Generators) })]`) picks them all
up. Weight branches with `Gen.Frequency` when one case is the common
path — a ~90 / 10 populated-vs-null split is a good default.
