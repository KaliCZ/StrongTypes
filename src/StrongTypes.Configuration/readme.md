# Kalicz.StrongTypes.Configuration

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.Configuration?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.Configuration?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

Makes an **unconfigured** strong type fail instead of quietly defaulting.

```csharp
builder.Services.AddOptions<RetryOptions>()
    .BindStrongTypes(builder.Configuration.GetSection("Retry"))
    .ValidateOnStart();
```

## The problem

[`Kalicz.StrongTypes`](https://www.nuget.org/packages/Kalicz.StrongTypes/) needs no package to
bind: every wrapper carries a `TypeConverter`, so values bind and invalid ones throw with the
invariant's own message. **This package is only about the key that isn't there.**

A wrapper's invariant constrains every value it can hold. It cannot make the binder assign one — the
binder reaches in through reflection and never calls `Create`. So:

```csharp
public sealed class RetryOptions
{
    public NonEmptyString Name { get; set; }        // unconfigured -> null
    public Positive<int> MaxRetries { get; set; }   // unconfigured -> 1
}
```

`ValidateOnStart()` does not help: binding an absent key *succeeds*, it just doesn't assign, so
nothing is raised. `[Required]` catches `Name`, because it is null — but **cannot** catch
`MaxRetries`, because `default(Positive<int>)` is `1`, an ordinary invariant-satisfying value that
looks exactly like a configured one. Neither does C#'s `required` keyword, which the binder's
reflection walks straight past.

## What it does

`BindStrongTypes()` binds the section, then asks **the configuration** whether each key is present
— rather than asking the bound object whether it looks null. That question has an answer for
structs:

```
OptionsValidationException: 'Retry:MaxRetries' is required but was not configured.
                            Declare RetryOptions.MaxRetries nullable if it is optional.
```

The declaration is the spec — no attributes:

| declaration                 | meaning     |
| --------------------------- | ----------- |
| `Positive<int> MaxRetries`  | required    |
| `Positive<int>? MaxRetries` | optional    |
| `NonEmptyString Name`       | required    |
| `NonEmptyString? Nickname`  | optional    |

Only Kalicz.StrongTypes wrappers are checked; a `string` or `int` property is left to whatever
validation you already use.

Pair it with `ValidateOnStart()`. On its own the failure is still lazy — it surfaces on the first
read of `IOptions<T>.Value`, not at startup.

## When you don't need it

- No strong types in your options classes.
- Every strong-typed property is nullable, so nothing is required.
- You already have an `IValidateOptions<T>` covering presence.

## Nullable reference types

Required-ness for a reference wrapper is read from the assembly's nullable annotations. An options
class in a project with `<Nullable>disable</Nullable>` carries none, so its reference wrappers are
treated as **optional** — with no intent declared there is nothing to enforce. Struct wrappers
(`Positive<int>` vs `Positive<int>?`) are unaffected: that distinction is in the type itself and
always readable.
