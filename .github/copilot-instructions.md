# GitHub Copilot Instructions

Full-stack web application template: .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

## Full Context

Read these files for detailed conventions:

- `AGENTS.md` — Project overview, architecture, agent workflow, git discipline
- `src/backend/AGENTS.md` — .NET patterns: entities, EF Core, Result pattern, services, controllers
- `src/frontend/AGENTS.md` — SvelteKit patterns: API client, type generation, components, state

## Key Rules

### Backend (.NET 10 / C# 13)
- Use `Result` / `Result<T>` for fallible operations — never throw for business logic
- C# 13 extension member syntax for all extension methods
- Primary constructors for DI, `internal` implementations
- Entities extend `BaseEntity`, configurations extend `BaseEntityConfiguration<T>`
- XML docs on controller actions — they generate the OpenAPI spec consumed by frontend

### Frontend (SvelteKit / Svelte 5)
- Svelte 5 Runes only — never `export let`
- Never hand-edit `v1.d.ts` — run `npm run api:generate`
- Barrel exports from feature folders, logical CSS properties only
- No `any` type, no working around missing API endpoints

### Workflow
- Conventional Commits, atomic commits after each logical change
- Pre-commit: `dotnet build` (backend), `npm run format && npm run lint && npm run check` (frontend)
- Session docs in `docs/sessions/` with Mermaid diagrams at end of each session
