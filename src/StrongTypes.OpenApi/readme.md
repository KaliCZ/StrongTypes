# Kalicz.StrongTypes.OpenApi

OpenAPI schema plumbing for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).

Out of the box `Microsoft.AspNetCore.OpenApi` describes the raw CLR shape of
strong types — so a `NonEmptyString` property shows up as a class with a
`Value` field, `Positive<int>` as a wrapper object, and `Maybe<T>` as the full
union surface. This package registers schema transformers that rewrite those
schemas to match the JSON the converters actually emit, so generated clients
and API explorers see the real wire format.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.OpenApi
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
