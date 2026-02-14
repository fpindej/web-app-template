# Agent Guidelines

Full-stack web application template: **.NET 10 API** (Clean Architecture) + **SvelteKit frontend** (Svelte 5), fully dockerized.

> Hard rules and pre-commit checks are in [`CLAUDE.md`](CLAUDE.md) — always loaded into context.

## Tech Stack

| | Backend | Frontend |
|---|---|---|
| **Framework** | .NET 10 / C# 13 | SvelteKit / Svelte 5 (Runes) |
| **Database** | PostgreSQL + EF Core | — |
| **Caching** | Redis (IDistributedCache) | — |
| **Auth** | JWT in HttpOnly cookies + permission claims | Cookie-based (automatic via API proxy) |
| **Authorization** | `[RequirePermission]` + role hierarchy | `hasPermission()` + `hasAnyPermission()` utilities |
| **Validation** | FluentValidation + Data Annotations | TypeScript strict mode |
| **API Docs** | Scalar (OpenAPI at `/openapi/v1.json`) | openapi-typescript (generated types) |
| **Styling** | — | Tailwind CSS 4 + shadcn-svelte (bits-ui) |
| **i18n** | — | paraglide-js (type-safe, compile-time) |
| **Logging** | Serilog → Seq | — |

## Architecture

```
Frontend (SvelteKit :5173)
    │
    │  /api/* proxy (catch-all server route, forwards cookies + headers)
    ▼
Backend API (.NET :8080)
    │
    ├── PostgreSQL (:5432)
    ├── Redis (:6379)
    ├── Hangfire (PostgreSQL-backed)
    └── Seq (:80)
```

### Backend — Clean Architecture

```
WebApi → Application ← Infrastructure
              ↓
           Domain
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, value objects, `Result` pattern. Zero dependencies. |
| **Application** | Interfaces, DTOs (Input/Output), service contracts. References Domain only. |
| **Infrastructure** | EF Core, Identity, Redis, service implementations. References Application + Domain. |
| **WebApi** | Controllers, middleware, validation, request/response DTOs. Entry point. |

### Frontend — SvelteKit

| Directory | Responsibility |
|---|---|
| `src/routes/(app)/` | Authenticated pages (redirect guard in layout) |
| `src/routes/(public)/` | Public pages (login) |
| `src/routes/api/` | API proxy to backend |
| `src/lib/api/` | Type-safe API client + generated OpenAPI types |
| `src/lib/components/` | Feature-organized components with barrel exports |
| `src/lib/state/` | Reactive state (`.svelte.ts` files) |
| `src/lib/config/` | App configuration (client-safe vs server-only split) |

## Code Quality Principles

These apply across the entire codebase — backend and frontend alike.

### Keep Structures Clean

Public methods should read like a table of contents — delegate implementation details to well-named private methods. If a method does three things, it should call three methods, not contain three inline blocks.

### Deduplicate When It Improves Clarity

When the same pattern appears more than once, extract it. But don't abstract prematurely — duplication is cheaper than the wrong abstraction. Extract when:

- The duplicated logic is **identical in intent**, not just similar in shape
- A change to one copy would always require the same change to the others
- The extracted method has a **clear, descriptive name** that improves readability

If the "shared" code would need parameters, flags, or conditionals to handle different callers, it's not real duplication — leave it inline.

### Design for Testability

Write code that is naturally testable through good structure, not by over-abstracting for the sake of mocking:

- **Small, focused methods** — easier to test in isolation
- **Constructor injection** — dependencies are explicit and swappable
- **Pure logic where possible** — methods that take inputs and return outputs without side effects are trivially testable
- **Don't wrap framework types just to mock them** — if testing requires mocking `HttpContext` or `DbContext`, use integration tests instead of creating abstraction layers that exist only for unit tests

---

## Security-First Development

**Security is the highest priority in every development decision.** When faced with a trade-off between convenience and security, always choose security. When unsure whether something is safe, assume it isn't and investigate.

### Guiding Principles

| Principle | What it means in practice |
|---|---|
| **Restrictive by default** | Deny access, disable features, block origins, strip headers — then selectively open what's needed. Never start permissive and try to lock down later. |
| **Defense in depth** | Don't rely on a single layer. Validate on both frontend and backend. Check auth in middleware *and* controllers. Use security headers *and* CSP. |
| **Least privilege** | Services, tokens, cookies, and API responses should expose the minimum data and permissions required. |
| **Fail closed** | If validation fails, token parsing fails, or an origin check fails — reject the request. Never fall through to a permissive default. |
| **Secrets never in code** | Connection strings, API keys, JWT secrets — always in `.env` or environment variables, never in source. Rotate compromised secrets immediately. |
| **Audit new dependencies** | Before adding a NuGet package or npm module, consider its attack surface. Prefer well-maintained, minimal-dependency libraries. |

### When Building Features

1. **Think about abuse first.** Before implementing, ask: how could this be exploited? What happens if the input is malicious? What if the user is unauthenticated?
2. **Validate all input.** Never trust client data — validate on the backend even if the frontend already validates. Use FluentValidation for complex rules, Data Annotations for simple constraints.
3. **Sanitize all output.** Prevent XSS by escaping user-generated content. Never render raw HTML from user input. Validate URLs to block `javascript:` schemes.
4. **Protect state-changing operations.** All mutations (POST/PUT/DELETE) must verify authentication, authorization, and CSRF protection. The SvelteKit API proxy validates Origin headers; the backend validates JWT tokens.
5. **Log security events.** Failed login attempts, token refresh failures, authorization denials — these should be logged at Warning/Error level for monitoring.

---

## Agent Workflow

### Git Discipline

**Commit continuously and atomically.** Every logically complete unit of work gets its own commit immediately — do not accumulate changes and commit at the end.

#### Conventional Commits

```
feat(auth): add refresh token rotation
fix(profile): handle null phone number in validation
refactor(persistence): extract pagination into extension method
chore: update NuGet packages
docs: add session notes for orders feature
test(auth): add login integration tests
```

Format: `type(scope): lowercase imperative description` — max 72 chars, no period. Scope is optional but encouraged. Add a body to explain *why* when the reason isn't obvious.

#### Atomic Commit Strategy

One commit = one logical change that could be reverted independently.

| ✅ Good (atomic) | ❌ Bad (bundled) |
|---|---|
| `feat(orders): add Order entity and EF config` | `feat: add entire orders feature` |
| `feat(orders): add IOrderService and DTOs` | (entity + service + controller + frontend |
| `feat(orders): implement OrderService` | all in one massive commit) |
| `feat(orders): add OrdersController with endpoints` | |
| `feat(orders): add order list page in frontend` | |

#### Pre-Commit Checks

Before **every** commit, verify the code compiles and passes checks:

- **Backend**: `dotnet build src/backend/MyProject.slnx`
- **Frontend**: `cd src/frontend && npm run format && npm run lint && npm run check`

Never commit code that doesn't compile, has lint errors, or fails type checks.

### Session Documentation

When the user asks to wrap up or create session docs, generate a documentation file:

- **Location**: `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md`
- **Template**: See [`docs/sessions/README.md`](docs/sessions/README.md) for the required structure
- **Commit**: As the final commit of the session: `docs: add session notes for {topic}`

Do **not** generate session docs automatically — only when explicitly requested.

#### When to Use Mermaid Diagrams

Include diagrams in session docs when they add clarity:

| Diagram Type | Use For |
|---|---|
| `flowchart TD` | Request/data flows, layer interactions |
| `erDiagram` | Entity relationships |
| `sequenceDiagram` | Multi-step flows (auth, token refresh) |
| `classDiagram` | Service/interface relationships |
| `stateDiagram-v2` | State transitions (order lifecycle) |

Keep diagrams focused — one concern per diagram, prefer a few clear diagrams over many trivial ones.

### Branch Hygiene

Work on the current branch unless instructed otherwise. For new branches: `feat/{name}` or `fix/{description}`.

### Issues

When creating GitHub issues, use `gh issue create` with:

- **Title**: Conventional Commit format (`type(scope): description`)
- **Body**: Problem description, proposed fix, and affected files
- **Labels**: Apply all relevant labels from the table below

#### Breaking Down Large Issues

When an issue spans multiple layers (backend + frontend), involves multiple logical steps, or could realistically be worked on by different developers in parallel, break it into **sub-issues**. The parent issue describes the overall goal; sub-issues are independently deliverable units of work.

Use `gh issue create` for each sub-issue, then link them to the parent using the **GitHub Sub-Issues API**:

```bash
# 1. Create the parent issue
gh issue create --title "feat(auth): add change password endpoint" \
  --body "..." --label "backend,frontend,feature"

# 2. Create each sub-issue
gh issue create --title "feat(auth): add change password endpoint (backend)" \
  --body "..." --label "backend,feature"
gh issue create --title "feat(auth): add change password form (frontend)" \
  --body "..." --label "frontend,feature"

# 3. Get the sub-issue's numeric ID (not the issue number)
gh api --method GET /repos/{owner}/{repo}/issues/{sub_issue_number} --jq '.id'

# 4. Link each sub-issue to the parent
gh api --method POST /repos/{owner}/{repo}/issues/{parent_number}/sub_issues \
  --field sub_issue_id={sub_issue_id}
```

This gives GitHub native progress tracking on the parent issue (completion count and percentage) rather than relying on markdown task lists.

> **Do not** use markdown checkbox task lists (`- [ ] #101`) to track sub-issues. Always use the Sub-Issues API so GitHub tracks hierarchy and progress natively.

**When to split:**

| Signal | Example |
|---|---|
| Crosses stack boundary | Backend endpoint + frontend page → separate issues |
| Independent deliverables | Database migration + service + controller could each be reviewed alone |
| Multiple logical concerns | New entity + new API + new UI page + new i18n keys |
| Parallelizable work | Two developers could work on different sub-issues simultaneously |

**When NOT to split:**

- Small, tightly coupled changes that only make sense together (e.g., adding a DTO and its validator)
- Single-layer fixes that take one commit (e.g., fixing a typo, adding an index)

Each sub-issue gets its own branch, PR, and labels — same conventions as any other issue. The parent issue is closed when all sub-issues are done.

### Pull Requests

When the user asks to create a PR, use `gh pr create` with:

- **Title**: Conventional Commit format matching the branch scope
- **Body**: Summary of changes, linked issues if applicable
- **Base**: `master` (unless instructed otherwise)
- **Labels**: Apply all relevant labels from the table below

Do **not** create PRs automatically — only when explicitly requested.

#### Merging PRs

Always use **squash and merge** (`gh pr merge --squash`). This collapses all branch commits into a single commit on `master`, keeping the main branch history clean and linear. Provide a meaningful squash commit message:

- **`--subject`**: Conventional Commit format summarizing the entire PR
- **`--body`**: Brief description of what changed and why, plus `Closes #N` if applicable

```bash
gh pr merge <number> --squash \
  --subject "fix(auth): add per-endpoint rate limiting for registration" \
  --body "Add dedicated fixed-window rate limiter for registration endpoint.
Closes #53"
```

Never use regular merge commits or rebase-merge — squash is the only merge strategy for this project.

### Labels

Always label issues and PRs. Use the project labels below — apply **all** that fit (they are not mutually exclusive). If a new label would genuinely help categorize work and none of the existing ones cover it, create it with `gh label create` before applying.

| Label | Color | Description | Use when |
|---|---|---|---|
| `backend` | `#0E8A16` | Backend (.NET) | Changes touch `src/backend/` |
| `frontend` | `#1D76DB` | Frontend (SvelteKit) | Changes touch `src/frontend/` |
| `security` | `#D93F0B` | Security-related | Fixes vulnerabilities, hardens config, adds auth features |
| `feature` | `#5319E7` | New feature or enhancement | Adding new capabilities (not just fixing existing ones) |
| `bug` | `#d73a4a` | Something isn't working | Fixing incorrect behavior |
| `documentation` | `#0075ca` | Documentation | Changes to docs, AGENTS.md, session notes |

Unused GitHub default labels (`enhancement`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`, `duplicate`) can be ignored — they add noise for a small team. Delete them if they accumulate.

---

## Error Handling Philosophy

| Layer | Strategy |
|---|---|
| **Backend services** | Return `Result` / `Result<T>` with descriptive `ErrorMessages.*` constant for expected failures |
| **Backend exceptions** | `KeyNotFoundException` → 404, `PaginationException` → 400, unhandled → 500 |
| **Backend middleware** | `ExceptionHandlingMiddleware` catches all, returns `ErrorResponse` JSON with `message` |
| **Frontend API errors** | `isValidationProblemDetails()` → field-level errors with shake animation |
| **Frontend generic errors** | `getErrorMessage()` uses backend's `message` field directly |
| **Frontend network errors** | `isFetchErrorWithCode('ECONNREFUSED')` → 503 "Backend unavailable" |

### Error Message Flow

The backend returns descriptive English messages directly. No error codes, no frontend translation of error codes — the message is the user-facing string:

```
Backend service
  → Result.Failure(ErrorMessages.Auth.LoginInvalidCredentials)
  → Controller returns ErrorResponse { message: "Invalid username or password." }

Frontend getErrorMessage()
  → uses the `message` field directly (backend messages are specific and user-friendly)
```

For dynamic messages (containing runtime values like usernames or role names), services use inline string interpolation instead of constants.

**Adding a new error message end-to-end:**

1. Add `const string` to `ErrorMessages.cs` in the appropriate nested class (Domain) — the value is the user-facing English message
2. Use it in the service's `Result.Failure()` call (Infrastructure)

## Local Development

```bash
# First time (or new machine where init script has already run):
cp .env.example .env

# Start all services
docker compose -f docker-compose.local.yml up -d

# API docs (development only)
open http://localhost:{INIT_API_PORT}/scalar/v1

# Seq logs
open http://localhost:{INIT_SEQ_PORT}
```

### Environment Configuration

`.env.example` contains working dev defaults for every variable. Copy it to `.env` and you're ready — no edits required. The init script does this automatically (and generates a random JWT secret), but a plain `cp` works too since `.env.example` includes a static dev key.

The API container loads `.env` in two ways:

1. **Variable interpolation** — `docker-compose.local.yml` references `${VAR}` / `${VAR:-default}` for values that need renaming or host-specific defaults (connection strings, secrets, ports).
2. **`env_file: .env`** — every variable in `.env` is also injected into the API container directly. ASP.NET picks up any `Section__Key` variable (e.g. `Authentication__Jwt__ExpiresInMinutes`) automatically.

#### Precedence (highest to lowest)

| Priority | Source | Example |
|---|---|---|
| 1 | `docker compose run --env` | CLI override (rare) |
| 2 | Compose `environment` block | `Authentication__Jwt__Key: ${JWT_SECRET_KEY}` — interpolated from `.env`, set via `environment` |
| 3 | Compose `env_file: .env` | `Authentication__Jwt__ExpiresInMinutes=100` — passes through directly |
| 4 | `appsettings.{Environment}.json` | `ExpiresInMinutes: 100` in `appsettings.Development.json` |
| 5 | `appsettings.json` | Base defaults (e.g. `ExpiresInMinutes: 10`) |

**In practice:** variables in the compose `environment` block (connection strings, secrets, Seq URL) always win. Everything else set in `.env` passes through to the container and overrides appsettings values. When running from Rider/VS (no Docker), only appsettings files apply — `.env` is not read.

#### What lives where

| File | Purpose | Who edits it |
|---|---|---|
| `.env.example` | Working dev defaults — copy to `.env` to get started | Rarely edited |
| `.env` | Local overrides (git-ignored) — copied from `.env.example` | Everyone |
| `appsettings.json` | Base/production defaults | Backend devs |
| `appsettings.Development.json` | Dev defaults (generous JWT expiry, debug logging, localhost URLs) | Backend devs |
| `docker-compose.local.yml` | Docker service wiring (host-specific values only) | Rarely edited |

### Developer Workflows

#### New machine setup

```bash
cp .env.example .env
docker compose -f docker-compose.local.yml up -d
```

That's it. `.env.example` has working defaults for everything.

#### Frontend dev — tweak backend config

Edit `.env` (not `.env.example`), uncomment and change what you need, restart Docker:

```bash
# Example: longer JWT tokens, relaxed rate limit
Authentication__Jwt__ExpiresInMinutes=300
RateLimiting__Global__PermitLimit=1000
```

```bash
docker compose -f docker-compose.local.yml up -d
```

No backend source files need to be touched.

#### Backend dev — just run everything

```bash
docker compose -f docker-compose.local.yml up -d
```

No config changes needed. Defaults work out of the box.

## Infrastructure & Tooling

### SDK & Runtime

| Tool | Version | Configured in |
|---|---|---|
| .NET SDK | `10.0.100` (`rollForward: latestMajor`) | `global.json` |
| Node.js | Engine-strict enforced | `src/frontend/package.json` + `.npmrc` |
| dotnet-ef | `10.0.0` | `.config/dotnet-tools.json` |

### Build Configuration

| File | Purpose |
|---|---|
| `Directory.Build.props` | Shared project properties: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable` |
| `Directory.Packages.props` | Centralized NuGet version management — all package versions defined here, not in `.csproj` files |
| `nuget.config` | Locked to NuGet.org only (no custom feeds) |
| `global.json` | Pins .NET SDK version |
| `src/frontend/.npmrc` | `engine-strict=true` — npm refuses to install if Node version doesn't match |

### CI/CD & Hooks

GitHub Actions workflows enforce quality gates on every PR and push to `master`:

| Workflow | File | Purpose |
|---|---|---|
| **CI** | `.github/workflows/ci.yml` | Build + lint + type-check, gated by `dorny/paths-filter` so backend-only PRs skip frontend and vice versa. Single `ci-passed` gate job for branch protection. |
| **Docker** | `.github/workflows/docker.yml` | Validates Docker images build successfully (no push). Triggered only when Dockerfiles or dependency manifests change. Uses GHA layer cache. |

**Dependabot** (`.github/dependabot.yml`) opens weekly PRs for NuGet, npm, and GitHub Actions updates. Minor+patch versions are grouped to reduce noise.

Pre-commit checks (build, format, lint, type check) remain manual steps documented in `CLAUDE.md` — CI enforces them server-side as a safety net.

### Docker

| File | Purpose |
|---|---|
| `src/backend/MyProject.WebApi/Dockerfile` | Multi-stage production build (restore → build → publish → runtime) |
| `src/frontend/Dockerfile` | Production build (build → runtime with Node adapter) |
| `src/frontend/Dockerfile.local` | Development — mounts source, runs `npm run dev` for hot-reload |
| `docker-compose.local.yml` | 5-service local stack (api, frontend, db, redis, seq) |

The backend Dockerfile uses layer caching: `.csproj` files are restored first (cached), then source is copied and built. This avoids re-downloading NuGet packages on every source change.

## Deployment

Build and push images via `./deploy.sh` (or `deploy.ps1`), configured by `deploy.config.json`.
