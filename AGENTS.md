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

### Localized Error Messages

The backend **never** sends human-readable error text. Instead, it returns machine-readable **error codes** (e.g. `"validation.required"`, `"identity.duplicateEmail"`) that the frontend resolves to localized strings via paraglide-js.

```
Backend (error codes) → API response → Frontend (resolves to locale-specific text)
```

This design means:
- Any future client (mobile, CLI) can use the same codes with its own i18n system
- Adding a new language only requires updating frontend message files — zero backend changes
- Error messages are never hardcoded in English on the backend

#### Two Error Response Formats

| Format | HTTP Status | Shape | Used By |
|---|---|---|---|
| `ValidationProblemDetails` | 400 | `{ errors: { "Email": ["validation.required"] } }` | Data Annotations + FluentValidation |
| `ErrorResponse` | 400/401 | `{ message: "identity.duplicateEmail, identity.passwordRequiresDigit" }` | `Result.Failure()` in services |

Both formats are handled transparently by the frontend's `mapFieldErrors()` and `getErrorMessage()` utilities — components don't need to know about error codes.

#### Adding a New Error Code (Full-Stack Checklist)

1. **Backend**: Add constant to `Application/Errors/ErrorCodes.cs`
2. **Backend**: Use the constant in the service/controller (via `Result.Failure()` or data annotation `ErrorMessage`)
3. **Frontend**: Add entry to `ERROR_CODE_MAP` in `error-handling.ts`
4. **Frontend**: Add translations to `en.json` and `cs.json` (key: `errorCode_{domain}_{code}`)

See [`src/backend/AGENTS.md`](src/backend/AGENTS.md) and [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) for detailed conventions.

## Local Development

```bash
# Start all services
docker compose -f docker-compose.local.yml up -d

# API docs (development only)
open http://localhost:13002/scalar/v1

# Seq logs
open http://localhost:13008
```

## Deployment

Build and push images via `./deploy.sh` (or `deploy.ps1`), configured by `deploy.config.json`.
