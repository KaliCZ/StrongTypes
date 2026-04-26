# Swashbuckle — `Kalicz.StrongTypes.OpenApi.Swashbuckle`

For apps wired to **`Swashbuckle.AspNetCore`** (`AddSwaggerGen()`). If
you use Microsoft's built-in **`AddOpenApi()`** instead, see
`references/openapi.md` — pick the package that matches your spec
generator. The two pipelines use disjoint extension points
(`ISchemaFilter` vs `IOpenApiSchemaTransformer`), so filters built for
one do nothing for the other.

Targets `Swashbuckle.AspNetCore` 10.x and the `Microsoft.OpenApi` 2.x
types it pulls in.

One call when configuring Swagger generation:

```csharp
builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
```

`AddStrongTypes()` registers schema filters that rewrite the generated
schema for every strong-type wrapper to match the JSON its converter
actually emits. Without it, Swashbuckle describes the raw CLR shape and
generated clients see nonsense for `NonEmptyString`, `Positive<int>`,
`Maybe<T>`, etc.

## What it produces

| C# type                                                           | OpenAPI schema                                                                  |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `NonEmptyString`                                                  | `{ "type": "string", "minLength": 1 }`                                          |
| `Positive<T>`                                                     | underlying primitive with `minimum: 0, exclusiveMinimum: true`                  |
| `NonNegative<T>`                                                  | underlying primitive with `minimum: 0`                                          |
| `Negative<T>`                                                     | underlying primitive with `maximum: 0, exclusiveMaximum: true`                  |
| `NonPositive<T>`                                                  | underlying primitive with `maximum: 0`                                          |
| `NonEmptyEnumerable<T>` / `INonEmptyEnumerable<T>`                | `{ "type": "array", "minItems": 1, "items": <T schema> }`                       |
| `Maybe<T>`                                                        | `{ "type": "object", "properties": { "Value": <T schema> } }`                   |

The `Maybe<T>` filter unwraps `Nullable<Maybe<T>>` internally — that
matters because `Maybe<T>` implements `IEnumerable<T>`, so a
`Maybe<T>?`-typed property would otherwise be coerced into an array
shape before any filter can see it.

## UI

Swashbuckle bundles Swagger UI as a separate package
(`Swashbuckle.AspNetCore.SwaggerUI`, included via the umbrella
`Swashbuckle.AspNetCore`). `app.UseSwaggerUI()` mounts the browseable
view at `/swagger`; the JSON spec is at `/swagger/v1/swagger.json`.

## Pairing with EF Core

The Swashbuckle and EF Core packages are independent — install whichever
combinations you need:

```csharp
builder.Services.AddSwaggerGen(options => options.AddStrongTypes());
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseStrongTypes());
```
