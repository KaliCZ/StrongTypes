# Kalicz.StrongTypes.OpenApi.Microsoft

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.OpenApi.Microsoft?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Microsoft/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.OpenApi.Microsoft?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Microsoft/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

OpenAPI schema plumbing for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).

Out of the box `Microsoft.AspNetCore.OpenApi` describes the raw CLR shape of
strong types — so a `NonEmptyString` property shows up as a class with a
`Value` field, `Positive<int>` as a wrapper object, and `Maybe<T>` as the full
union surface. This package registers schema transformers that rewrite those
schemas to match the JSON the converters actually emit, so generated clients
and API explorers see the real wire format.

If your app uses Swashbuckle (`AddSwaggerGen()`) instead, install
[`Kalicz.StrongTypes.OpenApi.Swashbuckle`](https://www.nuget.org/packages/Kalicz.StrongTypes.OpenApi.Swashbuckle)
— it provides the same schema corrections against Swashbuckle's pipeline.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.OpenApi.Microsoft
```

## Register

```csharp
builder.Services.AddOpenApi(options => options.AddStrongTypes());

var app = builder.Build();
app.MapOpenApi();
```

## What it does

- `NonEmptyString` &rarr; `{ "type": "string", "minLength": 1 }`
- `Positive<T>` &rarr; underlying primitive with `exclusiveMinimum: 0`
- `NonNegative<T>` &rarr; underlying primitive with `minimum: 0`
- `Negative<T>` &rarr; underlying primitive with `exclusiveMaximum: 0`
- `NonPositive<T>` &rarr; underlying primitive with `maximum: 0`
- `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>` &rarr;
  `{ "type": "array", "minItems": 1, "items": <T schema> }`
- `Maybe<T>` &rarr; object wrapper `{ "Value": <T schema, nullable> }` matching the
  converter's on-the-wire format.

## Data annotations

Both ASP.NET Core OpenAPI pipelines drop every `ValidationAttribute` on a
property whose CLR type carries a custom `JsonConverter` &mdash; which every
strong-type wrapper does &mdash; collapsing the property to a bare `$ref` to
the wrapper component.

This package re-applies them: every annotation
`Microsoft.AspNetCore.OpenApi` natively supports on a primitive-typed
property also works on a wrapper-typed one. Caller bounds compose with the
wrapper's floor under tighter-wins rules: the wrapper never relaxes a
caller's constraint, and a caller bound that would loosen the wrapper's
floor is ignored.

```csharp
public sealed record CreateUserRequest(
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$")]
    NonEmptyString Username,

    [Range(18, 120)]
    Positive<int> Age,

    [MaxLength(10)]
    NonEmptyEnumerable<NonEmptyString> Tags,

    [Url]
    NonEmptyString Website,

    [Description("Short user tagline")]
    NonEmptyString Tagline);
```

On the wire:

| Property | Resulting schema |
| --- | --- |
| `Username` | `{ "type": "string", "minLength": 3, "maxLength": 50, "pattern": "^[a-zA-Z0-9_]+$" }` |
| `Age` | `{ "type": "integer", "format": "int32", "minimum": 18, "maximum": 120 }` |
| `Tags` | `{ "type": "array", "minItems": 1, "maxItems": 10, "items": { "type": "string", "minLength": 1 } }` |
| `Website` | `{ "type": "string", "minLength": 1, "format": "uri" }` |
| `Tagline` | `{ "type": "string", "minLength": 1, "description": "Short user tagline" }` |

A few attributes are intentionally not propagated because the underlying
pipeline doesn't honor them on a primitive-typed property either, and the
wrapper-typed surface stays consistent with the primitive-typed surface
on each pipeline:

- `[EmailAddress]` &mdash; `Microsoft.AspNetCore.OpenApi` doesn't write
  `format: "email"` for it. The Swashbuckle adapter does propagate it
  (because Swashbuckle's `DataAnnotationsSchemaFilter` does).
- `[DefaultValue]` &mdash; the framework's own default-value handler
  crashes when the attribute's underlying value (e.g. `string`) doesn't
  match the property's declared wrapper type (e.g. `NonEmptyString`).
  Apply `[DefaultValue]` only to primitive-typed properties.
