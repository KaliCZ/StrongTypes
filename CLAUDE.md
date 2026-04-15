# StrongTypes — Coding Conventions

## Audience

This repository has multiple contributors. Do not rely on per-user memory,
personal preferences, or prior-session context when working here. Every
convention that matters belongs in this file (or in the code itself) so that
every collaborator — human or AI — sees the same rules.

## Migration from _Old

The codebase is mid-migration. Anything under a folder or filename suffixed with
`_Old` is legacy and should not be modified or extended. New code goes in
non-suffixed folders (e.g., new string types go in `src/StrongTypes/Strings/`).

When you rework an _Old type, treat the rewrite as a real review — drop the
`_Old` suffix, read the entire file, and improve anything that is wrong or
outdated, not just what the task names. Do not port code verbatim.

**Dealing with the _Old file being replaced:**

- If the old file is *entirely* about the type being reworked (e.g.,
  `NonEmptyString_Old.cs`), delete it. The new file is the replacement.
- If the old file mixes multiple concerns (e.g., `StringExtensions_Old.cs`
  contains extensions for both `string` and `NonEmptyString`), leave the
  `_Old` file in place and create a new non-suffixed file containing *only*
  the reworked pieces. The remaining legacy surface stays where it was until
  its own migration pass.
- In either case, minimally patch remaining `_Old` call sites only as much as
  is needed to keep the build green.

**Other rules:**

- Do **not** take a dependency on other _Old types (e.g., `Option<T>`). Prefer
  modern BCL primitives (nullable reference types, `Result`-shaped APIs, etc.).
- Do not delete _Old files unless explicitly asked or unless the rule above
  applies.

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

## Tests

- Prefer data-driven tests with `[Theory]` + `[InlineData]` (or `[MemberData]`
  when rows get complex) over duplicated `[Fact]` methods whose bodies differ
  only in input or expected value. One parameterized method with several
  data rows reads better and keeps assertion shape consistent across cases.
- Use separate `[Fact]` methods when the test body genuinely differs — e.g.
  asserting a side effect like "the factory was invoked exactly once".

## Style

- Prefer `Edit` over rewriting whole files.
- Keep public API changes minimal and documented.
- Do not add files, abstractions, or configurability beyond what the task asks for.
