# CLAUDE.md

Full-stack web application template: .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

## Read These for Full Context

- `AGENTS.md` — Project overview, architecture, agent workflow, git discipline
- `src/backend/AGENTS.md` — .NET conventions, entities, Result pattern, EF Core, services, controllers
- `src/frontend/AGENTS.md` — SvelteKit conventions, API client, type generation, components, state

## Hard Rules

- Never edit `src/frontend/src/lib/api/v1.d.ts` — run `npm run api:generate` instead
- Always use `Result` / `Result<T>` for backend operations that can fail
- Always use C# 13 extension member syntax for new extension methods
- Always use Svelte 5 Runes (`$props`, `$state`, `$derived`, `$effect`) — never `export let`
- Commit atomically using Conventional Commits after each logical change
- Run `dotnet build src/backend/MyProject.slnx` and `cd src/frontend && npm run format && npm run lint && npm run check` before every commit
- At session end, create `docs/sessions/{date}-{topic}.md` with summary, decisions, and Mermaid diagrams
