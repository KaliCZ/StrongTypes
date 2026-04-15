# StrongTypes — Claude instructions

## C# conventions

- **Primary constructors** — use them wherever possible (classes, DbContext subclasses, test fixtures, etc.)
- All new code targets `net10.0` / C# 14

## Integration test conventions

- **Requests** — always use anonymous objects (`new { Value = "x", NullableValue = (string?)null }`), never the typed request records from the API project.
- **Responses** — parse with `ReadFromJsonAsync<JsonElement>()` and pull fields by name (e.g. `json.GetProperty("id").GetGuid()`), never deserialize into a typed response record.
- **Rationale** — keeps tests decoupled from the API's model types; the test verifies the HTTP contract (field names, status codes) and the DB state, not the C# type system.

## Documentation and memory policy

All project context, decisions, and notes belong in this file. Do **not** save anything to the external memory system (`~/.claude/projects/…`) — no project memories, no feedback memories, nothing. If something is worth remembering about this repo, it goes here so it travels with the code.

## Project overview

A C# library (`Kalicz.StrongTypes`, v0.2.0) that reduces boilerplate and prevents bugs through stronger typing. Based on FuncSharp by Honza Siroky. Targets `net10.0`, `LangVersion 14.0`.

All existing source files carry an `_Old` suffix — they were renamed in commit `42ae5be` as a "prepare for migration" step. New code added after that point is the first non-`_Old` code.

## Solution structure (`src/`)

| Project | Purpose |
|---|---|
| `StrongTypes` | Core library — no external dependencies |
| `StrongTypes.Tests` | Unit tests: xUnit 2.9.3 + FsCheck 3.3.2 |
| `StrongTypes.Examples` | Usage examples |
| `StrongTypes.Benchmarks` | BenchmarkDotNet benchmarks |
| `StrongTypes.Api` | ASP.NET Core minimal API (test harness for serialization) |
| `StrongTypes.Api.IntegrationTests` | Integration tests using Testcontainers |

## StrongTypes.Api — integration test harness

Added 2026-04-15. Purpose: verify that strong types serialize correctly through the ASP.NET Core request pipeline and EF Core (SQL Server + PostgreSQL).

**Current state:** uses plain `string` / `string?`. Strong-type converters (EF Core value converters, JSON converters) will be wired in once parallel work on those lands.

### Entity

`StringEntity` — `Guid Id`, `string Value`, `string? NullableValue`

### Endpoints

| Method | Route | Notes |
|---|---|---|
| POST | `/string-entities/non-nullable` | `Value` + `NullableValue` both required |
| POST | `/string-entities/nullable` | `Value` required, `NullableValue?` optional |
| PUT | `/string-entities/{id}/non-nullable` | `Value` + `NullableValue` both required |
| PUT | `/string-entities/{id}/nullable` | `Value` required, `NullableValue?` optional |

Each endpoint writes to **both** DbContexts before returning.

### Test infrastructure

- `TestWebApplicationFactory` — `ICollectionFixture` + `IAsyncLifetime`; starts SQL Server and PostgreSQL Testcontainers in parallel, overrides DbContext registrations, calls `EnsureCreated` on both.
- `IntegrationTestCollection` — `[CollectionDefinition]` so all test classes share one factory instance (containers start once).
- 6 tests across `CreateItemTests` and `UpdateItemTests`; every test asserts values read back from **both** databases.

### Package versions (estimates — verify on restore)

- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0
- `Microsoft.AspNetCore.Mvc.Testing` 10.0.0
- `Testcontainers.MsSql` 4.0.0
- `Testcontainers.PostgreSql` 4.0.0
