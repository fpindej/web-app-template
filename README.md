<div align="center">

# NETrock

**The production-grade SaaS foundation you'd eventually build yourself — so you don't have to.**

.NET 10 + SvelteKit + PostgreSQL + Redis. Clean Architecture. Fully dockerized.
One script. Zero boilerplate debt.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SvelteKit](https://img.shields.io/badge/SvelteKit-Svelte_5-FF3E00?logo=svelte&logoColor=white)](https://svelte.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

</div>

---

## Why NETrock?

Every SaaS starts the same way: authentication, role management, rate limiting, validation, API docs, type-safe clients, i18n, Docker setup... You spend weeks on infrastructure before writing a single line of business logic.

**NETrock is the foundation that skips all of that.** Not a toy starter kit — a production-hardened architecture with real security, real patterns, and real conventions that scale.

Run the init script, pick a name, and start building your product. Everything else is already done.

---

## What You Get

### Backend — .NET 10 / C# 13

| Feature | Implementation |
|---|---|
| **Clean Architecture** | Domain → Application → Infrastructure → WebApi, with enforced dependency rules |
| **Authentication** | JWT in HttpOnly cookies, refresh token rotation, security stamp validation, remember-me persistent sessions |
| **Authorization** | Permission-based with custom roles — atomic permissions (`users.view`, `roles.manage`, …) assigned per role, enforced via `[RequirePermission]` |
| **Rate Limiting** | Global + per-endpoint policies, configurable fixed-window with IP partitioning |
| **Validation** | FluentValidation + Data Annotations, flowing constraints into OpenAPI spec |
| **Caching** | Redis (distributed) + in-memory fallback, cache-aside pattern with auto-invalidation |
| **Database** | PostgreSQL + EF Core with soft delete, audit fields, and global query filters |
| **API Documentation** | OpenAPI spec + Scalar UI, with enum handling and schema transformers |
| **Error Handling** | Result pattern for business logic, middleware for exceptions, structured `ErrorResponse` everywhere |
| **Logging** | Serilog → Seq with structured request logging |
| **Account Management** | Registration, login/logout, remember me, password change, profile updates, account deletion |
| **Admin Panel** | User management, custom role CRUD with permission editor, role assignment, hierarchy enforcement |
| **Background Jobs** | Hangfire with PostgreSQL persistence — recurring jobs via `IRecurringJobDefinition`, fire-and-forget via `IBackgroundJobClient`, admin UI with trigger/pause/resume/restore/delete, persistent pause state |
| **Soft Refresh** | Role/permission changes rotate security stamps but preserve refresh tokens — users silently re-authenticate without logout |

### Frontend — SvelteKit / Svelte 5

| Feature | Implementation |
|---|---|
| **Svelte 5 Runes** | Modern reactivity with `$state`, `$derived`, `$effect` — no legacy stores |
| **Type-Safe API Client** | Generated from OpenAPI spec via `openapi-typescript` — backend changes break the build, not the user |
| **Tailwind CSS 4** | Utility-first styling with shadcn-svelte (bits-ui) components |
| **BFF Architecture** | Server-side API proxy handles cookies, CSRF, and auth transparently |
| **i18n** | Paraglide JS — type-safe keys, compile-time optimization, SSR-compatible |
| **Security Headers** | CSP-ready, X-Frame-Options, Referrer-Policy, Permissions-Policy on every response |
| **Permission Guards** | Frontend route and component guards driven by JWT permission claims — pages, nav items, and actions gated per permission |
| **Responsive Layout** | Sidebar navigation with mobile drawer, breakpoint-aware page layouts |
| **Admin UI** | User list with search, role list with create/edit/delete, permission checkbox editor grouped by category, job dashboard with trigger/pause/resume |

### Infrastructure

| Feature | Implementation |
|---|---|
| **Fully Dockerized** | One `docker compose up` for the entire stack — API, frontend, DB, Redis, Seq |
| **Init Script** | Renames the entire solution, sets ports, generates secrets, restores tools |
| **Environment Config** | `.env` overrides for everything — JWT expiry, rate limits, CORS, Redis, logging |
| **Deploy Script** | Build and push images with a single command |
| **Structured Logging** | Seq dashboard for searching, filtering, and correlating requests |

---

## Architecture

```
Frontend (SvelteKit :5173)
    │
    │  /api/* proxy (catch-all server route)
    │  Forwards cookies + headers, validates CSRF
    ▼
Backend API (.NET :8080)
    │
    │  Clean Architecture
    │  WebApi → Application ← Infrastructure → Domain
    │
    ├── PostgreSQL (:5432)  — EF Core, soft delete, audit trails, Hangfire storage
    ├── Redis (:6379)       — Distributed cache, security stamp lookup
    ├── Hangfire            — Recurring + fire-and-forget background jobs
    └── Seq (:80)           — Structured log aggregation
```

---

## Security — Not an Afterthought

NETrock is built **security-first**. Every decision defaults to the most restrictive option, then selectively opens what's needed.

- **JWT in HttpOnly cookies** — tokens never touch JavaScript, immune to XSS theft
- **Refresh token rotation** — single-use tokens with automatic family revocation on reuse detection
- **Security stamp validation** — permission changes propagate to active sessions via SHA-256 hashed stamps in JWT claims, cached in Redis
- **Soft refresh** — role/permission changes invalidate access tokens but preserve refresh tokens, so users silently re-authenticate instead of getting kicked out
- **Permission-based authorization** — atomic permissions enforced on every admin endpoint via `[RequirePermission]`, with frontend guards on routes and UI components
- **Role hierarchy protection** — users cannot escalate privileges; only SuperAdmin can assign Admin roles, system roles cannot be deleted
- **CORS production safeguard** — startup guard rejects `AllowAllOrigins` in non-development environments, preventing credential-leaking misconfiguration from reaching production
- **Rate limiting** — global + per-endpoint (registration has its own stricter policy), configurable per environment
- **CSRF protection** — Origin header validation in the SvelteKit API proxy
- **Input validation everywhere** — FluentValidation on the backend, even if the frontend already validates
- **Security headers on every response** — both API and frontend set `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`
- **Soft delete** — nothing is ever truly gone, audit trail on every mutation

---

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/)
- [Git](https://git-scm.com/)

### 1. Clone & Initialize

```bash
git clone https://github.com/fpindej/web-app-template.git my-saas
cd my-saas
```

**macOS / Linux:**
```bash
chmod +x init.sh
./init.sh
```

**Windows (PowerShell):**
```powershell
.\init.ps1
```

The init script will:
1. Ask for your **project name** (e.g., `Acme`)
2. Ask for a **base port** (default `13000`)
3. Rename all files, directories, namespaces, and configs
4. Generate a random JWT secret
5. Restore .NET tools (`dotnet-ef`)

### 2. Launch Everything

```bash
docker compose -f docker-compose.local.yml up -d --build
```

That's it. Your entire stack is running:

| Service | URL |
|---|---|
| **Frontend** | `http://localhost:<BASE_PORT>` |
| **API** | `http://localhost:<BASE_PORT + 2>` |
| **API Docs (Scalar)** | `http://localhost:<BASE_PORT + 2>/scalar/v1` |
| **Seq (Logs)** | `http://localhost:<BASE_PORT + 8>` |

### 3. Start Building

The foundation is in place. Add your domain entities, services, and pages — the architecture guides you:

```
# Add a backend feature
src/backend/YourProject.Domain/Entities/         → Entity
src/backend/YourProject.Application/Features/    → Interface + DTOs
src/backend/YourProject.Infrastructure/Features/ → Implementation
src/backend/YourProject.WebApi/Features/         → Controller + Validation

# Add a frontend page
src/frontend/src/routes/(app)/your-feature/      → Page + components
src/frontend/src/lib/api/                        → Auto-generated types
```

---

## Project Structure

```
src/
├── backend/                          # .NET 10 API (Clean Architecture)
│   ├── YourProject.Domain/           # Entities, value objects, Result pattern
│   ├── YourProject.Application/      # Interfaces, DTOs, service contracts
│   ├── YourProject.Infrastructure/   # EF Core, Identity, Redis, implementations
│   └── YourProject.WebApi/           # Controllers, middleware, validation
│
└── frontend/                         # SvelteKit application
    └── src/
        ├── lib/
        │   ├── api/                  # Generated OpenAPI types + client
        │   ├── components/           # shadcn-svelte + feature components
        │   ├── state/                # Reactive state (.svelte.ts)
        │   └── config/              # App configuration
        └── routes/
            ├── (app)/                # Authenticated pages
            │   └── admin/            # Admin panel (users, roles, permissions, jobs)
            ├── (public)/             # Public pages (login)
            └── api/                  # API proxy to backend
```

---

## Developer Workflows

### Frontend dev — tweak backend config without touching code

Edit `.env`, restart Docker:

```bash
# Longer JWT tokens, relaxed rate limit
Authentication__Jwt__ExpiresInMinutes=300
RateLimiting__Global__PermitLimit=1000
```

```bash
docker compose -f docker-compose.local.yml up -d
```

### Backend dev — debug with breakpoints in Rider/VS

1. Stop the API container: `docker compose -f docker-compose.local.yml stop api`
2. Set `API_URL=http://host.docker.internal:5142` in `.env`
3. Restart frontend: `docker compose -f docker-compose.local.yml restart frontend`
4. Launch API from your IDE — breakpoints work, frontend proxies to it

### Database migrations

```bash
dotnet ef migrations add <Name> \
  --project src/backend/<YourProject>.Infrastructure \
  --startup-project src/backend/<YourProject>.WebApi \
  --output-dir Features/Postgres/Migrations
```

Migrations auto-apply on startup in Development.

---

## Localization

Production-ready i18n with [Paraglide JS](https://inlang.com/m/gerre34r/library-inlang-paraglideJs):

- **Type-safe keys** — generated from `en.json`, compile-time errors on missing keys
- **SSR-compatible** — correct `lang` attribute on first render, no hydration mismatch
- **Auto-detection** — browser language → `Accept-Language` header → cookie fallback
- **Adding a language** — create `es.json`, register in `i18n.ts`, done

---

## What This Is NOT

NETrock is opinionated by design. It's not:

- **A generic starter** — it makes real choices (PostgreSQL, not "any database"; JWT cookies, not "pluggable auth")
- **A microservices framework** — it's a monolith, because that's what 95% of SaaS products should start as
- **A UI kit** — it uses shadcn-svelte components, but your product's design is your own
- **Magic** — you still need to understand .NET, SvelteKit, and SQL. NETrock gives you the architecture, not the knowledge

---

## License

This project is licensed under the [MIT License](LICENSE).
