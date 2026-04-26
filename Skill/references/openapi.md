# OpenAPI — `Kalicz.StrongTypes.OpenApi.Microsoft`

For apps wired to **`Microsoft.AspNetCore.OpenApi`** (`AddOpenApi()`).
If you use **Swashbuckle** (`AddSwaggerGen()`) instead, see
`references/swashbuckle.md` — pick the package that matches your spec
generator. The two are not interchangeable; transformers built for one
pipeline do nothing for the other.

One call when configuring `Microsoft.AspNetCore.OpenApi`:

```csharp
builder.Services.AddOpenApi(options => options.AddStrongTypes());

var app = builder.Build();
app.MapOpenApi();
```

`AddStrongTypes()` registers schema transformers that rewrite the
generated schema for every strong-type wrapper to match the JSON its
converter actually emits. Without it, `Microsoft.AspNetCore.OpenApi`
describes the raw CLR shape — `NonEmptyString` becomes an object with a
`Value` field, `Positive<int>` a wrapper object, `Maybe<T>` the full
union surface — and generated clients are unusable.

## What it produces

| C# type                                                           | OpenAPI schema                                                                  |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `NonEmptyString`                                                  | `{ "type": "string", "minLength": 1 }`                                          |
| `Positive<T>`                                                     | underlying primitive with `exclusiveMinimum: 0`                                 |
| `NonNegative<T>`                                                  | underlying primitive with `minimum: 0`                                          |
| `Negative<T>`                                                     | underlying primitive with `exclusiveMaximum: 0`                                 |
| `NonPositive<T>`                                                  | underlying primitive with `maximum: 0`                                          |
| `NonEmptyEnumerable<T>` / `INonEmptyEnumerable<T>`                | `{ "type": "array", "minItems": 1, "items": <T schema> }`                       |
| `Maybe<T>`                                                        | `{ "type": "object", "properties": { "Value": <T schema, nullable> } }`         |
| `IEnumerable<T>` where `T` is a strong-type wrapper               | `{ "type": "array", "items": <T schema> }` (no `minItems` — element schema only)|

## OpenAPI version

The transformers work for both OpenAPI 3.0 and 3.1. If you need 3.0
output (e.g. tooling that doesn't speak 3.1 yet), set the version on
the same options object:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    options.AddStrongTypes();
});
```

In 3.0, `exclusiveMinimum` / `exclusiveMaximum` emit as booleans paired
with `minimum` / `maximum`; in 3.1 they emit as numeric values. Both
forms are produced correctly by the transformers — the version setting
is the only knob.

## UI

`AddOpenApi()` produces only the JSON document at `/openapi/v1.json`.
There is no built-in UI; pair it with Scalar (`Scalar.AspNetCore`),
`Swashbuckle.AspNetCore.SwaggerUI` (UI half only, pointed at the
existing JSON), Redoc, or NSwag UI when you want a browseable view.

## Pairing with EF Core

The OpenAPI and EF Core packages are independent — install whichever
combinations you need:

```csharp
builder.Services.AddOpenApi(options => options.AddStrongTypes());
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseStrongTypes());
```
