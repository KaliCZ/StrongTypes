# `Email`

A validated e-mail address wrapping `System.Net.Mail.MailAddress`, capped at
254 characters (the RFC 5321 deliverable limit, `Email.MaxLength`). A sealed
reference type; equality is case-insensitive on the full address.

## Construction

```csharp
Email? maybe = Email.TryCreate(input);   // null when invalid, too long, or null
Email  email = Email.Create(input);      // throws ArgumentException when invalid

// The string extensions produce the BCL MailAddress instead:
MailAddress? parsed = input.AsEmail();   // null when invalid
MailAddress  strict = input.ToEmail();   // throws when invalid
```

`MailAddress` converts to `Email` implicitly (`Email e = mailAddress;`), and
`Email` converts implicitly to both `string` (the address) and `MailAddress`,
so it passes straight into APIs that take either.

## What you get

- `Value` — the underlying `MailAddress` (use it for `.User` / `.Host`).
- `Address` — the address string; `ToString()` returns the same.
- Case-insensitive equality (`==` / `!=`) against `Email`, `MailAddress`,
  `string`, and `NonEmptyString`. `CompareTo` for ordering — there are no
  ordering operators.
- `IParsable<Email>`, a JSON converter, and a `TypeConverter`, so JSON
  bodies, `IConfiguration` binding, and WPF/WinForms two-way binding all
  work with no setup.

## JSON

Serialises as a plain JSON string (the address). An invalid or too-long
string throws `JsonException`; JSON `null` yields a null reference even for
a non-nullable declaration — nullability is compile-time only.

## Integrations

- **EF Core** — maps to a string column via `Kalicz.StrongTypes.EfCore`;
  plain `MailAddress` properties are converted too (`references/efcore.md`).
- **OpenAPI** — both adapters render
  `{ "type": "string", "format": "email", "minLength": 1, "maxLength": 254 }`
  (`references/openapi.md`).
- **Options binding** — a non-nullable `Email` left null by a missing key is
  caught by `BindStrongTypes()` (`references/configuration.md`).
- **FsCheck** — `Generators` ships `Email` / `NullableEmail` / `MaybeEmail`
  arbitraries (`references/fscheck.md`).
