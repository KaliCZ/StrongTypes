# Kalicz.StrongTypes.OpenApi.Swashbuckle &nbsp;&nbsp;&nbsp;&nbsp; [![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

[![StrongTypes downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes?label=downloads%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/)
[![StrongTypes.EfCore downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/)
[![StrongTypes.FsCheck downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.FsCheck?label=downloads%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)
[![StrongTypes.OpenApi.Microsoft downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.OpenApi.Microsoft?label=downloads%20%28StrongTypes.OpenApi.Microsoft%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Microsoft/)
[![StrongTypes.OpenApi.Swashbuckle downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.OpenApi.Swashbuckle?label=downloads%20%28StrongTypes.OpenApi.Swashbuckle%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Swashbuckle/)

Swashbuckle (Swagger) schema plumbing for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).

Out of the box `Swashbuckle.AspNetCore` describes the raw CLR shape of strong
types — so a `NonEmptyString` property shows up as a class with a `Value` field,
`Positive<int>` as a wrapper object, and `Maybe<T>` as the full union surface.
This package registers schema filters that rewrite those schemas to match the
JSON the converters actually emit, so generated clients and Swagger UI see the
real wire format.

If your app uses `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()`) instead of
Swashbuckle, install [`Kalicz.StrongTypes.OpenApi.Microsoft`](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Microsoft)
— it provides the same schema corrections against Microsoft's pipeline.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.OpenApi.Swashbuckle
```

## Register

```csharp
builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
```

## What it does

- `NonEmptyString` &rarr; `{ "type": "string", "minLength": 1 }`
- `Positive<T>` &rarr; underlying primitive with `minimum: 0, exclusiveMinimum: true`
- `NonNegative<T>` &rarr; underlying primitive with `minimum: 0`
- `Negative<T>` &rarr; underlying primitive with `maximum: 0, exclusiveMaximum: true`
- `NonPositive<T>` &rarr; underlying primitive with `maximum: 0`
- `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>` &rarr;
  `{ "type": "array", "minItems": 1, "items": <T schema> }`
- `Maybe<T>` &rarr; object wrapper `{ "Value": <T schema> }` matching the
  converter's on-the-wire format.

## Data annotations

Both ASP.NET Core OpenAPI pipelines drop every `ValidationAttribute` on a
property whose CLR type carries a custom `JsonConverter` &mdash; which every
strong-type wrapper does &mdash; collapsing the property to a bare `$ref` to
the wrapper component.

This package re-applies them: every annotation Swashbuckle's
`DataAnnotationsSchemaFilter` natively writes for a primitive-typed property
(including any third-party schema filter you've registered) also reaches a
wrapper-typed one. Caller bounds compose with the wrapper's floor under
tighter-wins rules: the wrapper never relaxes a caller's constraint, and a
caller bound that would loosen the wrapper's floor is ignored.

```csharp
public sealed record CreateUserRequest(
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$")]
    NonEmptyString Username,

    [Range(18, 120)]
    Positive<int> Age,

    [MaxLength(10)]
    NonEmptyEnumerable<NonEmptyString> Tags,

    [EmailAddress]
    NonEmptyString ContactEmail,

    [Url]
    NonEmptyString Website);
```

On the wire:

| Property | Resulting schema |
| --- | --- |
| `Username` | `{ "type": "string", "minLength": 3, "maxLength": 50, "pattern": "^[a-zA-Z0-9_]+$" }` |
| `Age` | `{ "type": "integer", "format": "int32", "minimum": 18, "maximum": 120 }` |
| `Tags` | `{ "type": "array", "minItems": 1, "maxItems": 10, "items": { "type": "string", "minLength": 1 } }` |
| `ContactEmail` | `{ "type": "string", "minLength": 1, "format": "email" }` |
| `Website` | `{ "type": "string", "minLength": 1, "format": "uri" }` |

Annotations Swashbuckle's filter doesn't natively map on primitive-typed
properties &mdash; e.g. `[Description]`, `[DefaultValue]`, `[Length]`,
`[Base64String]`, `[Range(MinimumIsExclusive = true)]` &mdash; aren't
written here either, so the wrapper-typed surface stays consistent with
the primitive-typed one. If you also need those, install
[`Kalicz.StrongTypes.OpenApi.Microsoft`](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Microsoft)
and use `Microsoft.AspNetCore.OpenApi`.
