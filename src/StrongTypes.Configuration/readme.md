# Kalicz.StrongTypes.Configuration

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.Configuration?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.Configuration?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.Configuration/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

Stops an options class binding a non-nullable property to `null`.

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
binder reaches in through reflection and never calls `Create` — and for an absent key it assigns
nothing at all. So:

```csharp
public sealed class RetryOptions
{
    public NonEmptyString Name { get; set; } = null!;   // unconfigured -> null
}
```

`null` — which the declaration says is impossible. `ValidateOnStart()` does not help:
binding an absent key *succeeds*, it just doesn't assign, so nothing is raised. C#'s `required`
doesn't either — it's a compile-time rule and the binder's reflection walks past it. `[Required]`
does work, but only if you remember it on every property.

## What it does

Fails when a property **declared non-nullable** is null after binding. Your nullable reference
annotations already say which properties those are, so nothing has to repeat it:

```csharp
public NonEmptyString Name { get; set; } = null!;          // fails unless configured
public NonEmptyString? Nickname { get; set; }              // nullable — fine
public string ApiKey { get; set; } = null!;                // fails unless configured
public string Endpoint { get; set; } = "https://x.test";   // has a default — never null
```

```
OptionsValidationException: 'Retry:Name' is null. Configure it, give RetryOptions.Name a default,
                            or declare it nullable.
```

**Every reference property is covered, not only the wrappers** — a `string` declared non-nullable is
as broken by a missing key as a `NonEmptyString` is. A declared default needs no configuration and
no annotation: it simply isn't null.

**At every depth**, too: nested options classes, collection elements and dictionary values are walked,
and the failure names the full path (`'Retry:Endpoints:1:Host'`). The walk stops where the binder
stops — at any type it has a `TypeConverter` for — so a wrapper is a leaf, not a graph to descend into.

Pair it with `ValidateOnStart()`. On its own the failure is still lazy — it surfaces on the first
read of `IOptions<T>.Value`, not at startup.

## Value types are not checked, and don't need to be

An unconfigured `Positive<int>` is `1`; an unconfigured `bool` is `false`. Those are defaults, and
they are values those types are perfectly happy to hold — there is no contradiction to find. Only
`null` in something that promised never to be null is one, and only a reference can manage it.

So this will not tell you that you forgot to configure `MaxRetries`. If "not configured" has to be
distinguishable from a configured default, declare it `Positive<int>?` and check for null yourself
— that is the only way the CLR gives you to say it.

## When you don't need it

- Every reference property on your options classes is nullable or has a default.
- You already use `[Required]` with `ValidateDataAnnotations()`, which covers the same ground with
  an attribute per property.

## Nullable reference types

Intent is read from the assembly's nullable annotations. An options class in a project with
`<Nullable>disable</Nullable>` carries none, so nothing on it is enforced — with no intent declared
there is nothing to enforce.
