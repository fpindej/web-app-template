# CLAUDE.md

NETrock - .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

**The backend API is the core of the project.** It is a public-facing API designed to serve any client - multiple frontends, mobile apps, other backends, third-party integrations. The SvelteKit frontend is a fully functional reference client, not a throwaway. Treat every API change as if unknown consumers depend on it.

```
Frontend (SvelteKit :5173) → /api/* proxy → Backend API (.NET :8080) → PostgreSQL / MinIO
Backend layers: WebApi → Application ← Infrastructure → Domain + Shared
```

## Hard Rules

### Backend

- `Result`/`Result<T>` for all fallible operations - never throw for business logic
- `TimeProvider` (injected) - never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- C# 13 `extension(T)` syntax for new extension methods
- Never `null!` - fix the design instead
- `ProblemDetails` (RFC 9457) for all error responses - never anonymous objects or raw strings
- `internal` on all Infrastructure service implementations
- `/// <summary>` XML docs on all public and internal API surface
- `System.Text.Json` only - never `Newtonsoft.Json`
- NuGet versions in `Directory.Packages.props` only - never in `.csproj`

### Frontend

- Never hand-edit `v1.d.ts` - run `pnpm run api:generate`
- Svelte 5 Runes only - never `export let`
- `interface Props` + `$props()` - never `$props<{...}>()`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` - never `ml-*`/`mr-*`/`pl-*`/`pr-*`
- No `any` - define proper interfaces
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`
- Use shadcn-svelte components (`pnpm dlx shadcn-svelte@latest add <name>`) - never build what shadcn already provides
- Pixel-perfect responsiveness - mobile, tablet, desktop, ultrawide, landscape and portrait
- Touch targets minimum 44px on all interactive elements
- Unified UX - reuse existing components and patterns so the app feels like one product, not five
- No overflow - dialogs, modals, and pages must never show scrollbars. Fit content to viewport.

### Cross-Cutting

- Security first - when convenience and security conflict, choose security. Deny by default, open selectively. Full PII compliance (`users.view_pii` permission, server-side masking, no PII in logs/URLs/errors).
- Atomic commits: `type(scope): imperative description` (Conventional Commits). No `Co-Authored-By` lines.
- No dead code - remove unused imports, variables, functions, files, and stale references in the same commit
- No em dashes - never use `—` anywhere (code, comments, docs, UI). Use `-` or rewrite the sentence.

## Delegation Rule

The top-level agent is an orchestrator. It does not write application code in `src/` - that goes to specialized agents.

**Default (application code in `src/`):**
- Delegate implementation to `backend-engineer`, `frontend-engineer`, or `fullstack-engineer`
- Run relevant reviewers in parallel after implementation completes
- Run `filemap-checker` after modifying files with known consumers

**Orchestrator handles directly (no delegation needed):**
- Configuration and tooling files (`.claude/`, `CLAUDE.md`, `FILEMAP.md`, `.gitignore`)
- Quick doc edits (typos, small updates); substantial docs go to `tech-writer`
- Quick answers, planning, research, and code review
- Commits, PRs, and git operations

**Commit ownership:** subagents implement and verify but never commit. The orchestrator commits after reviewers pass - one commit per logical unit, using the commit messages the engineer agents suggest in their reports.

**User override:** If the user explicitly asks to skip delegation ("do it yourself", "directly", "don't delegate", "just fix it"), the orchestrator implements directly regardless of scope.

## Agent Team

All application code in `src/` goes to specialized agents. User override is the only exception (see Delegation Rule). Run reviewers in parallel after every implementation.

| Agent | Role | When to use |
|---|---|---|
| `backend-engineer` | Implements .NET features | Task stays within `src/backend/` |
| `frontend-engineer` | Implements SvelteKit features | Task stays within `src/frontend/` |
| `fullstack-engineer` | Implements cross-stack features | Task touches both backend and frontend |
| `backend-reviewer` | Audits C# code (read-only) | Reviewing backend changes |
| `frontend-reviewer` | Audits Svelte code (read-only) | Reviewing frontend changes |
| `ux-designer` | Audits UI/UX design quality (read-only) | Checking responsiveness, visual consistency, theming |
| `security-reviewer` | Audits for vulnerabilities (read-only) | Auth, permissions, PII, tokens, middleware changes |
| `devops-engineer` | Implements infra changes | Dockerfiles, Aspire, CI/CD, health checks, env vars |
| `devops-reviewer` | Audits infra/deployment (read-only) | Dockerfiles, compose, Aspire, CI/CD changes |
| `test-writer` | Writes tests | Tests needed alongside implementation |
| `filemap-checker` | Verifies downstream updates (read-only) | After modifying files with known consumers |
| `tech-writer` | Writes documentation | READMEs, session docs, guides |
| `product-owner` | Proposes prioritized work items (read-only) | Deciding what to work on next, backlog review |

**Delegation patterns:**
- **New backend feature**: `backend-engineer` implements, then `backend-reviewer` + `security-reviewer` audit in parallel
- **New frontend feature**: `frontend-engineer` implements, then `frontend-reviewer` + `ux-designer` audit in parallel
- **Full-stack feature**: `fullstack-engineer` implements end-to-end
- **PR review** (`/review-pr`): `backend-reviewer` + `frontend-reviewer` + `security-reviewer` in parallel, add `ux-designer` for UI changes
- **Design review**: `ux-designer` validates responsiveness and visual consistency
- **Infra task**: `devops-engineer` implements, then `devops-reviewer` + `security-reviewer` audit in parallel
- **Pre-release check**: `devops-reviewer` validates deployment readiness
- **What to work on next**: `product-owner` analyzes codebase, issues, and TODOs to propose prioritized work
- **After modifying shared files**: `filemap-checker` verifies all consumers updated

## Breaking Changes

The backend API is public-facing. Treat every contract change with the same care as a published library.

| Layer | Breaking change |
|---|---|
| **Domain entity** | Renaming/removing a property, changing a type |
| **Application interface/DTO** | Changing a method signature, renaming/removing a field, changing nullability |
| **WebApi endpoint** | Changing route, method, request/response shape, status codes |
| **Frontend API types** | Always regenerated - broken by any backend DTO change |
| **i18n keys** | Renaming a key (all usages break) |

**Pre-modification checklist:** (1) Check FILEMAP.md for impact, (2) Search all usages, (3) Regenerate frontend types if API changed, (4) Update i18n in the correct feature files for all configured locales, (5) Document in commit body. Prefer additive changes. If breaking, update all consumers in the same PR.

## Verification

Run before every commit. Fix all errors before committing. **Loop until green - never commit with failures.**

```bash
# Backend (run when src/backend/ changed)
dotnet build src/backend/MyProject.slnx && dotnet test src/backend/MyProject.slnx -c Release

# Frontend (run when src/frontend/ changed)
cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check

# Aspire (run to verify local orchestration - requires Docker)
dotnet run --project src/backend/MyProject.AppHost
```

## Autonomous Behaviors

Do these automatically - never wait to be asked:

| Trigger | Action |
|---|---|
| **Any code change** | Run relevant verification (backend/frontend/both). Fix failures. Loop until green. |
| **Modifying existing files** | Check FILEMAP.md for downstream impact before editing. Update all affected files in the same commit. |
| **Backend API change** (endpoint, DTO, response shape) | Regenerate frontend types (`pnpm run api:generate`), fix type errors. |
| **Logically complete unit of work** | Commit immediately with Conventional Commit message. Don't batch, don't ask. |
| **Creating a PR** (`/create-pr`) | Auto-generate session doc in `docs/sessions/` for non-trivial PRs (3+ commits or 5+ files). |
| **Adding any feature** | Write tests alongside the implementation - component, API integration, validator as applicable. |
| **Build/test failure** | Read the error, fix it, re-run. Repeat until green. Don't stop and report the error unless stuck after 3 attempts. |
| **Unclear requirement** | Infer from context and existing patterns first. Ask the user only when genuinely ambiguous (multiple valid approaches with different tradeoffs). |
| **Backend-only task** | Delegate to `backend-engineer` (unless user overrides). Run `backend-reviewer` + `security-reviewer` in parallel after. |
| **Frontend-only task** | Delegate to `frontend-engineer` (unless user overrides). Run `frontend-reviewer` + `ux-designer` in parallel after. |
| **Cross-stack task** | Delegate to `fullstack-engineer` (unless user overrides). Run relevant reviewers in parallel after. |
| **Infra-only task** (Dockerfiles, Aspire, CI/CD, env vars) | Delegate to `devops-engineer` (unless user overrides). Run `devops-reviewer` + `security-reviewer` in parallel after. |
| **After any implementation** | Run relevant reviewers in parallel (backend-reviewer, frontend-reviewer, security-reviewer, ux-designer as applicable). |
| **After modifying shared files** | Run `filemap-checker` to verify all downstream consumers are updated. |

## File Roles

| File | Contains |
|---|---|
| `.claude/agents/` | Specialized agents for delegation (engineers, reviewers, designers, writers) |
| `.claude/skills/` | Step-by-step procedures and convention references (type `/` to list user-invocable skills) |
| `.claude/rules/` | Path-scoped convention rules, auto-loaded when matching files are touched |
| `.claude/hooks/` | Lifecycle hooks: safety gates, auto-format, quality checks |
| `.claude/README.md` | How this whole setup fits together - read it once after cloning |
| `.mcp.json` | Project MCP servers (Playwright for browser verification) |
| `FILEMAP.md` | "When you change X, also update Y" - change impact tables |
