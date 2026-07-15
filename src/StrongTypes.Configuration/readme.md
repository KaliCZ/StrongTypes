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

The declaration is the spec — no attributes. A property is **required when it is not nullable and
the options class gives it no default of its own**:

```csharp
public NonEmptyString Name { get; set; } = null!;          // required — null! declares no default
public NonEmptyString? Nickname { get; set; }              // optional — nullable
public Positive<int> MaxRetries { get; set; }              // required
public Positive<int>? Score { get; set; }                  // optional — nullable
public string Endpoint { get; set; } = "https://x.test";   // optional — has a default
public string ApiKey { get; set; } = null!;                // required
public int Timeout { get; set; }                           // required
public int? Timeout { get; set; }                          // optional
```

**Every property is checked, not only the wrappers.** Opting in says this options class should be
fully configured, and a missing `string` is exactly as silent as a missing `NonEmptyString`. A
collection or nested object counts as configured when the section has it, value or children.

Pair it with `ValidateOnStart()`. On its own the failure is still lazy — it surfaces on the first
read of `IOptions<T>.Value`, not at startup.

## Two declarations it cannot read

- **A value type whose intended default is the CLR default.** `bool Enabled { get; set; } = false`
  is indistinguishable from `bool Enabled { get; set; }`, so it is required. Declare it `bool?` to
  make it optional.
- **A reference property in an assembly with `<Nullable>disable</Nullable>`.** It carries no
  annotation, so it is treated as **optional** — with no intent declared there is nothing to
  enforce. Struct properties are unaffected: `Positive<int>` vs `Positive<int>?` lives in the type
  and is always readable.

## When you don't need it

- Every property on your options classes is nullable or has a default, so nothing is required.
- You already have an `IValidateOptions<T>` covering presence.
