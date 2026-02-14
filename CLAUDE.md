# CLAUDE.md

NETrock — full-stack web app template: .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

## Architecture

```
Frontend (SvelteKit :5173) → /api/* proxy → Backend API (.NET :8080) → PostgreSQL / Redis / Seq
```

Backend: `WebApi → Application ← Infrastructure → Domain` (Clean Architecture)

## Hard Rules

### Backend

- `Result`/`Result<T>` for all fallible operations — never throw for business logic
- `TimeProvider` (injected) — never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- C# 13 `extension(T)` syntax for new extension methods
- Never `null!` — fix the design instead
- Typed DTOs only — `ErrorResponse` for errors, never anonymous objects or raw strings
- `internal` on all Infrastructure service implementations
- `/// <summary>` XML docs on all public and internal API surface
- `System.Text.Json` only — never `Newtonsoft.Json` (present solely as a Hangfire transitive dependency)
- NuGet versions in `Directory.Packages.props` only — never in `.csproj` files

### Frontend

- Never hand-edit `src/frontend/src/lib/api/v1.d.ts` — run `npm run api:generate`
- Svelte 5 Runes only: `$props`, `$state`, `$derived`, `$effect` — never `export let`
- `interface Props` + `$props()` — never `$props<{...}>()`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` — never `ml-*`/`mr-*`/`pl-*`/`pr-*`
- No `any` type — define proper interfaces
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`

### Cross-Cutting

- Security restrictive by default — deny first, open selectively
- Atomic commits using Conventional Commits: `type(scope): imperative description`

## Pre-Commit Checks

```bash
dotnet build src/backend/MyProject.slnx
cd src/frontend && npm run format && npm run lint && npm run check
```

## Conventions Reference

| File | When to read |
|---|---|
| `AGENTS.md` | Architecture, workflow, git discipline, security, error handling, local dev |
| `src/backend/AGENTS.md` | Entities, Result pattern, EF Core, services, controllers, validation, OpenAPI |
| `src/frontend/AGENTS.md` | Routing, API client, type generation, components, state, i18n, styling |
| `SKILLS.md` | Step-by-step recipes for common operations (add entity, endpoint, page, etc.) |
| `FILEMAP.md` | Change impact tables ("when you change X, also update Y") and file location index |

## Session Documentation

When explicitly asked: create `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md` per `docs/sessions/README.md`. Commit: `docs: add session notes for {topic}`.
