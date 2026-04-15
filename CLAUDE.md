# StrongTypes — Coding Conventions

## Migration from _Old

The codebase is mid-migration. Anything under a folder or filename suffixed with
`_Old` is legacy and should not be modified or extended. New code goes in
non-suffixed folders (e.g., new string types go in `src/StrongTypes/Strings/`).

When reimplementing an _Old type:
- Read the _Old version for API surface and test coverage expectations.
- Do **not** take a dependency on other _Old types (e.g., `Option<T>`). Prefer
  modern BCL primitives (nullable reference types, `Result`-shaped APIs, etc.).
- Do not delete _Old files unless explicitly asked.

## Validated value types — the TryCreate / Create pattern

For value types that enforce a validation rule (e.g., `NonEmptyString`,
`PositiveInt`), follow this factory pattern:

```csharp
// Returns null when validation fails. Caller handles the null case.
public static NonEmptyString? TryCreate(string? value) { ... }

// Throws when validation fails. Thin wrapper over TryCreate.
public static NonEmptyString Create(string? value)
    => TryCreate(value) ?? throw new ArgumentException(...);
```

Rules:
- The validation logic lives in `TryCreate` only. `Create` delegates.
- Constructors are `private` — callers must go through the factories.
- Use nullable reference types. The library project does not enable them
  globally yet, so add `#nullable enable` at the top of each new file.
- Do **not** return `Option<T>` from new code.

## Style

- Prefer `Edit` over rewriting whole files.
- Keep public API changes minimal and documented.
- Do not add files, abstractions, or configurability beyond what the task asks for.
