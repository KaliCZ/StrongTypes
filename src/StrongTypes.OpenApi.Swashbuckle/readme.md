# Kalicz.StrongTypes.OpenApi.Swashbuckle

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
