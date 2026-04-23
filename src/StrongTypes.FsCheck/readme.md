# Kalicz.StrongTypes.FsCheck [![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)
[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes/) [![StrongTypes downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes?label=downloads%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/) [![StrongTypes.EfCore downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/) [![StrongTypes.FsCheck downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.FsCheck?label=downloads%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)

FsCheck arbitraries for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
Lets you write property tests against code that takes or returns `NonEmptyString`,
`Digit`, `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`, `Maybe<T>`,
and `NonEmptyEnumerable<T>` without hand-rolling generators that re-derive each
type's invariants.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.FsCheck
```

## Register

Register everything with one attribute on your test class:

```csharp
using FsCheck.Xunit;
using StrongTypes.FsCheck;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MyTests
{
    [Property]
    public void NonEmptyString_round_trips_through_json(NonEmptyString value)
    {
        // value is guaranteed non-null, non-empty, non-whitespace
    }

    [Property]
    public void Positive_stays_positive(Positive<int> value)
    {
        Assert.True(value.Value > 0);
    }
}
```

## What ships

Scalar strong types ship three shapes: the type itself, its nullable form
(`T?`, ~5% `null`), and `Maybe<T>` (~5% `None`).

| Type                 | `T`             | `T?`                    | `Maybe<T>`              |
| -------------------- | --------------- | ----------------------- | ----------------------- |
| `NonEmptyString`     | `NonEmptyString`| `NullableNonEmptyString`| `MaybeNonEmptyString`   |
| `Digit`              | `Digit`         | `NullableDigit`         | `MaybeDigit`            |
| `Positive<int>`      | `PositiveInt`   | `NullablePositiveInt`   | `MaybePositiveInt`      |
| `Negative<int>`      | `NegativeInt`   | `NullableNegativeInt`   | `MaybeNegativeInt`      |
| `NonNegative<int>`   | `NonNegativeInt`| `NullableNonNegativeInt`| `MaybeNonNegativeInt`   |
| `NonPositive<int>`   | `NonPositiveInt`| `NullableNonPositiveInt`| `MaybeNonPositiveInt`   |

Apart from the above, you also get:

- `NonEmptyEnumerableInt` — `NonEmptyEnumerable<int>`
- `Maybe<T>` for common primitives: `MaybeBool`, `MaybeInt`, `MaybeLong`,
  `MaybeDouble`, `MaybeChar`, `MaybeString`, `MaybeGuid` — all with ~5% `None`.

Version matches the core `Kalicz.StrongTypes` package you install alongside it.
