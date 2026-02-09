# Agent Guidelines

Full-stack web application template: **.NET 10 API** (Clean Architecture) + **SvelteKit frontend** (Svelte 5), fully dockerized.

> For explanations, rationale, and design decisions, see [`docs/`](docs/README.md).

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

> For architecture rationale, data flow diagrams, and auth flow details, see [`docs/architecture.md`](docs/architecture.md).

## Detailed Conventions

| Area | Reference |
|---|---|
| Backend (.NET) | [`src/backend/AGENTS.md`](src/backend/AGENTS.md) |
| Frontend (SvelteKit) | [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) |

Read the relevant file before working in that area. Both are self-contained with real code examples.

---

## Code Quality Principles

> For rationale and examples, see [`docs/backend-conventions.md` — Code Quality Principles](docs/backend-conventions.md#code-quality-principles).

- **Keep structures clean** — public methods read like a table of contents; delegate details to well-named private methods.
- **Deduplicate when identical in intent** — extract when a change to one copy would always require the same change to the others. If the "shared" code needs flags or conditionals, it's not real duplication.
- **Design for testability** — small focused methods, constructor injection, pure logic where possible. Don't wrap framework types just to mock them.

---

## Security-First Development

**Security is the highest priority in every development decision.** When in doubt, choose the more restrictive option.

> For full security architecture (cookie design, CSP rationale, header explanations), see [`docs/security.md`](docs/security.md).

| Principle | Rule |
|---|---|
| **Restrictive by default** | Deny access, block origins, strip headers — then selectively open |
| **Defense in depth** | Validate on both frontend and backend; use headers *and* CSP |
| **Least privilege** | Expose minimum data and permissions |
| **Fail closed** | Reject on validation/parsing/origin failure — never fall through |
| **Secrets never in code** | `.env` or environment variables only |
| **Audit dependencies** | Consider attack surface before adding packages |

When building features:

1. **Think about abuse first** — how could this be exploited? What if the input is malicious?
2. **Validate all input** — never trust client data, validate on backend even if frontend validates
3. **Sanitize all output** — prevent XSS, never render raw HTML from user input, validate URLs
4. **Protect state-changing operations** — POST/PUT/DELETE must verify auth, authorization, and CSRF
5. **Log security events** — failed logins, token refresh failures, authorization denials at Warning/Error level

Layer-specific security rules:
- **Backend**: [`src/backend/AGENTS.md` — Security section](src/backend/AGENTS.md#security)
- **Frontend**: [`src/frontend/AGENTS.md` — Security section](src/frontend/AGENTS.md#security)

---

## Error Handling

| Layer | Strategy |
|---|---|
| **Backend services** | Return `Result` / `Result<T>` for expected failures |
| **Backend exceptions** | `KeyNotFoundException` → 404, `PaginationException` → 400, unhandled → 500 |
| **Backend middleware** | `ExceptionHandlingMiddleware` catches all, returns `ErrorResponse` JSON |
| **Frontend API errors** | `isValidationProblemDetails()` → field-level errors with shake animation |
| **Frontend generic errors** | `getErrorMessage()` → toast notification |
| **Frontend network errors** | `isFetchErrorWithCode('ECONNREFUSED')` → 503 "Backend unavailable" |

---

## Agent Workflow

> For detailed explanations, sub-issues API examples, and session doc templates, see [`docs/workflow.md`](docs/workflow.md).

### Git Discipline

**Commit continuously and atomically.** Every logically complete unit of work gets its own commit immediately.

#### Conventional Commits

```
feat(auth): add refresh token rotation
fix(profile): handle null phone number in validation
refactor(persistence): extract pagination into extension method
chore: update NuGet packages
docs: add session notes for orders feature
test(auth): add login integration tests
```

Format: `type(scope): lowercase imperative description` — max 72 chars, no period.

#### Atomic Commit Strategy

One commit = one logical change that could be reverted independently.

| ✅ Good (atomic) | ❌ Bad (bundled) |
|---|---|
| `feat(orders): add Order entity and EF config` | `feat: add entire orders feature` |
| `feat(orders): add IOrderService and DTOs` | (entity + service + controller + frontend |
| `feat(orders): implement OrderService` | all in one massive commit) |

#### Pre-Commit Checks

Before **every** commit:

- **Backend**: `dotnet build src/backend/MyProject.slnx`
- **Frontend**: `cd src/frontend && npm run format && npm run lint && npm run check`

Never commit code that doesn't compile, has lint errors, or fails type checks.

### Documentation Maintenance

When a PR changes conventions, architecture, patterns, or workflows, update the relevant `docs/` files and AGENTS.md files in the same PR.

| If you change... | Update |
|---|---|
| Backend patterns, entities, services, EF Core | `src/backend/AGENTS.md` + `docs/backend-conventions.md` |
| Frontend patterns, components, state, CSS | `src/frontend/AGENTS.md` + `docs/frontend-conventions.md` |
| Security headers, CSP, auth, cookies | Layer AGENTS.md + `docs/security.md` |
| API contract, DTOs, OpenAPI annotations | `src/backend/AGENTS.md` + `docs/api-contract.md` |
| Architecture, layers, request flow | `AGENTS.md` + `docs/architecture.md` |
| Dev setup, Docker, environment config | `docs/getting-started.md` |
| Git workflow, issues, PRs, labels | `AGENTS.md` + `docs/workflow.md` |

Rules:
- Update docs in the **same commit** as the code change they document (or as a dedicated `docs:` commit in the same PR)
- AGENTS.md changes = actionable rules, code templates, tables, checklists
- docs/ changes = explanations, rationale, design decisions
- If adding a new convention, add the rule to AGENTS.md **and** the rationale to docs/
- If removing or changing a convention, update **both** places

### Session Documentation

When the user asks to wrap up or create session docs:

- **Location**: `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md`
- **Template**: See [`docs/sessions/README.md`](docs/sessions/README.md) for the required structure
- **Commit**: `docs: add session notes for {topic}`

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
- **Labels**: Apply all relevant labels

Do **not** create PRs automatically — only when explicitly requested.

### Labels

Always label issues and PRs. Apply **all** that fit.

| Label | Color | Description | Use when |
|---|---|---|---|
| `backend` | `#0E8A16` | Backend (.NET) | Changes touch `src/backend/` |
| `frontend` | `#1D76DB` | Frontend (SvelteKit) | Changes touch `src/frontend/` |
| `security` | `#D93F0B` | Security-related | Fixes vulnerabilities, hardens config, adds auth features |
| `feature` | `#5319E7` | New feature or enhancement | Adding new capabilities |
| `bug` | `#d73a4a` | Something isn't working | Fixing incorrect behavior |
| `documentation` | `#0075ca` | Documentation | Changes to docs, AGENTS.md, session notes |

---

## Local Development

> For full setup instructions, developer workflows (Docker, IDE debugging, phone testing), and environment configuration, see [`docs/getting-started.md`](docs/getting-started.md).

Quick start:

```bash
cp .env.example .env
docker compose -f docker-compose.local.yml up -d
```

## Deployment

Build and push images via `./deploy.sh` (or `deploy.ps1`), configured by `deploy.config.json`.
