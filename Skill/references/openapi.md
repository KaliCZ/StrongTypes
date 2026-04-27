# OpenAPI — `Kalicz.StrongTypes.OpenApi.*`

Two adapter packages, one for each spec generator ASP.NET Core ships
with. Pick the one that matches the pipeline your app already uses —
they are not interchangeable, because the two pipelines use disjoint
extension points (`IOpenApiSchemaTransformer` vs `ISchemaFilter`), so
hooks built for one do nothing for the other.

| Spec generator                          | Package                                    | Configured on             |
| --------------------------------------- | ------------------------------------------ | ------------------------- |
| `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()`) | `Kalicz.StrongTypes.OpenApi.Microsoft`   | `OpenApiOptions`          |
| `Swashbuckle.AspNetCore` (`AddSwaggerGen()`)    | `Kalicz.StrongTypes.OpenApi.Swashbuckle` | `SwaggerGenOptions`       |

Both packages expose the same entry point — `options.AddStrongTypes()`
— which registers the schema transformers / filters that rewrite the
generated schema for every strong-type wrapper to match the JSON its
converter actually emits. Without it, the generator describes the raw
CLR shape — `NonEmptyString` becomes an object with a `Value` field,
`Positive<int>` a wrapper object, `Maybe<T>` the full union surface —
and generated clients are unusable.

The Swashbuckle package targets `Swashbuckle.AspNetCore` 10.x and the
`Microsoft.OpenApi` 2.x types it pulls in.

## Wiring it up

### Microsoft (`AddOpenApi`)

```csharp
builder.Services.AddOpenApi(options => options.AddStrongTypes());

var app = builder.Build();
app.MapOpenApi();
```

### Swashbuckle (`AddSwaggerGen`)

```csharp
builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
```

## What it produces

| C# type                                                           | OpenAPI schema                                                                  |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `NonEmptyString`                                                  | `{ "type": "string", "minLength": 1 }`                                          |
| `Positive<T>`                                                     | underlying primitive with `exclusiveMinimum: 0` (3.1) or `minimum: 0, exclusiveMinimum: true` (3.0) |
| `NonNegative<T>`                                                  | underlying primitive with `minimum: 0`                                          |
| `Negative<T>`                                                     | underlying primitive with `exclusiveMaximum: 0` (3.1) or `maximum: 0, exclusiveMaximum: true` (3.0) |
| `NonPositive<T>`                                                  | underlying primitive with `maximum: 0`                                          |
| `NonEmptyEnumerable<T>` / `INonEmptyEnumerable<T>`                | `{ "type": "array", "minItems": 1, "items": <T schema> }`                       |
| `Maybe<T>`                                                        | `{ "type": "object", "properties": { "Value": <T schema> } }`                   |
| `IEnumerable<T>` where `T` is a strong-type wrapper               | `{ "type": "array", "items": <T schema> }` (no `minItems` — element schema only) |

The `Maybe<T>` filter unwraps `Nullable<Maybe<T>>` internally — that
matters because `Maybe<T>` implements `IEnumerable<T>`, so a
`Maybe<T>?`-typed property would otherwise be coerced into an array
shape before any filter can see it.

## OpenAPI version

The transformers / filters work for both OpenAPI 3.0 and 3.1.

For Microsoft's pipeline, if you need 3.0 output (e.g. tooling that
doesn't speak 3.1 yet), set the version on the same options object:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    options.AddStrongTypes();
});
```

In 3.0, `exclusiveMinimum` / `exclusiveMaximum` emit as booleans paired
with `minimum` / `maximum`; in 3.1 they emit as numeric values. Both
forms are produced correctly — the version setting is the only knob.

Swashbuckle 10.x emits 3.0 by default; switch via its own options
(`SwaggerGeneratorOptions.OpenApiVersion`) if you need 3.1.

## UI

`AddOpenApi()` produces only the JSON document at `/openapi/v1.json`.
There is no built-in UI; pair it with Scalar (`Scalar.AspNetCore`),
`Swashbuckle.AspNetCore.SwaggerUI` (UI half only, pointed at the
existing JSON), Redoc, or NSwag UI when you want a browseable view.

Swashbuckle bundles Swagger UI as a separate package
(`Swashbuckle.AspNetCore.SwaggerUI`, included via the umbrella
`Swashbuckle.AspNetCore`). `app.UseSwaggerUI()` mounts the browseable
view at `/swagger`; the JSON spec is at `/swagger/v1/swagger.json`.

## Pairing with EF Core

The OpenAPI and EF Core packages are independent — install whichever
combinations you need:

```csharp
builder.Services.AddOpenApi(options => options.AddStrongTypes());
// or: builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseStrongTypes());
```
