# GitHub Copilot Instructions

NETrock — full-stack web app template: .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

## Full Context

Read these files for detailed conventions:

- `AGENTS.md` — Architecture, workflow, git discipline, security, error handling, local dev
- `src/backend/AGENTS.md` — .NET patterns: entities, EF Core, Result pattern, services, controllers
- `src/frontend/AGENTS.md` — SvelteKit patterns: API client, type generation, components, state

## Key Rules

### Backend (.NET 10 / C# 13)

- `Result`/`Result<T>` for all fallible operations — never throw for business logic
- `TimeProvider` (injected) — never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- C# 13 `extension(T)` syntax for all new extension methods
- Never `null!` — fix the design instead
- Typed DTOs only — `ErrorResponse` for errors, never anonymous objects or raw strings
- `internal` on all Infrastructure service implementations
- `/// <summary>` XML docs on all public and internal API surface
- NuGet versions in `Directory.Packages.props` only — never in `.csproj` files
- Entities extend `BaseEntity`, configurations extend `BaseEntityConfiguration<T>`

### Frontend (SvelteKit / Svelte 5)

- Svelte 5 Runes only: `$props`, `$state`, `$derived`, `$effect` — never `export let`
- `interface Props` + `$props()` — never `$props<{...}>()`
- Never hand-edit `v1.d.ts` — run `npm run api:generate`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` — never physical (`ml-*`/`mr-*`)
- No `any` type — define proper interfaces
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`

### Cross-Cutting

- Security restrictive by default — deny first, open selectively
- Atomic commits using Conventional Commits: `type(scope): imperative description`
- Pre-commit: `dotnet build` (backend), `npm run format && npm run lint && npm run check` (frontend)
- Session docs in `docs/sessions/` — create when asked, not automatically
- PRs via `gh pr create` — create when asked, not automatically
