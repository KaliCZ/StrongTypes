# StrongTypes — Claude instructions

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

`Item` — `Guid Id`, `string Name`, `string? Description`

### Endpoints

| Method | Route | Notes |
|---|---|---|
| POST | `/items/non-nullable` | `Name` + `Description` both required |
| POST | `/items/nullable` | `Name` required, `Description?` optional |
| PUT | `/items/{id}/non-nullable` | `Name` + `Description` both required |
| PUT | `/items/{id}/nullable` | `Name` required, `Description?` optional |

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
