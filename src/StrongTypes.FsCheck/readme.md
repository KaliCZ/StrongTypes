# Kalicz.StrongTypes.FsCheck

[![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml)<br>
[![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)<br>
[![Kalicz.StrongTypes on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes?label=nuget%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/)<br>
[![Kalicz.StrongTypes downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes?label=downloads%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/)<br>
[![Kalicz.StrongTypes.EfCore on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes.EfCore?label=nuget%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/)<br>
[![Kalicz.StrongTypes.EfCore downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/)<br>
[![Kalicz.StrongTypes.FsCheck on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes.FsCheck?label=nuget%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)<br>
[![Kalicz.StrongTypes.FsCheck downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.FsCheck?label=downloads%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)

FsCheck arbitraries for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
Lets you write property tests against code that takes or returns `NonEmptyString`,
`Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`, `Maybe<T>`, and
`NonEmptyEnumerable<T>` without hand-rolling generators that re-derive each type's
invariants.

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

- `NonEmptyString` — filtered to non-null, non-whitespace values
- `NullableNonEmptyString` — ~10% null injection
- `Positive<int>`, `Negative<int>`, `NonNegative<int>`, `NonPositive<int>`
- `Maybe<int>`, `Maybe<string>`, `Maybe<NonEmptyString>`, `Maybe<Positive<int>>` —
  ~20% `None` injection
- `NonEmptyEnumerable<int>`

Version matches the core `Kalicz.StrongTypes` package you install alongside it.
