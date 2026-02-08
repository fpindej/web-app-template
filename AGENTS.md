# Agent Guidelines

Full-stack web application template: **.NET 10 API** (Clean Architecture) + **SvelteKit frontend** (Svelte 5), fully dockerized.

## Tech Stack

| | Backend | Frontend |
|---|---|---|
| **Framework** | .NET 10 / C# 13 | SvelteKit / Svelte 5 (Runes) |
| **Database** | PostgreSQL + EF Core | — |
| **Caching** | Redis (IDistributedCache) | — |
| **Auth** | JWT in HttpOnly cookies | Cookie-based (automatic via API proxy) |
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

## Detailed Conventions

| Area | Reference |
|---|---|
| Backend (.NET) | [`src/backend/AGENTS.md`](src/backend/AGENTS.md) |
| Frontend (SvelteKit) | [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) |

Read the relevant file before working in that area. Both are self-contained with real code examples.

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

### Pull Requests

When the user asks to create a PR, use `gh pr create` with:

- **Title**: Conventional Commit format matching the branch scope
- **Body**: Summary of changes, linked issues if applicable
- **Base**: `master` (unless instructed otherwise)

Do **not** create PRs automatically — only when explicitly requested.

---

## Error Handling Philosophy

| Layer | Strategy |
|---|---|
| **Backend services** | Return `Result` / `Result<T>` for expected failures |
| **Backend exceptions** | `KeyNotFoundException` → 404, `PaginationException` → 400, unhandled → 500 |
| **Backend middleware** | `ExceptionHandlingMiddleware` catches all, returns `ErrorResponse` JSON |
| **Frontend API errors** | `isValidationProblemDetails()` → field-level errors with shake animation |
| **Frontend generic errors** | `getErrorMessage()` → toast notification |
| **Frontend network errors** | `isFetchErrorWithCode('ECONNREFUSED')` → 503 "Backend unavailable" |

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

#### Backend dev — debug in Rider/VS with frontend

1. Stop the API container: `docker compose -f docker-compose.local.yml stop api`
2. In `.env`, uncomment: `API_URL=http://host.docker.internal:5142`
3. Restart the frontend container: `docker compose -f docker-compose.local.yml restart frontend`
4. Launch the API from Rider with the "Development - http" profile (port 5142)
5. Use the frontend at `localhost:{INIT_FRONTEND_PORT}` — it proxies API calls to Rider

The backend loads `appsettings.Development.json` which has `localhost` connection strings for db/redis/seq (pointing at Docker-exposed ports). Breakpoints work.

#### Backend dev — just run everything

```bash
docker compose -f docker-compose.local.yml up -d
```

No config changes needed. Defaults work out of the box.

## Deployment

Build and push images via `./deploy.sh` (or `deploy.ps1`), configured by `deploy.config.json`.
