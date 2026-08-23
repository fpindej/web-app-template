<div align="center">

# NETrock

**Full-stack .NET 10 + SvelteKit foundation for building real products.**

OAuth with 10 providers. TOTP two-factor auth. Admin-configurable everything. Thoroughly tested. API-first - use the included frontend or bring your own.

[![Generator](https://img.shields.io/badge/Generator-netrock.dev-FF6B35?logo=rocket&logoColor=white)](https://netrock.dev)
[![CI](https://github.com/fpindej/netrock/actions/workflows/ci.yml/badge.svg)](https://github.com/fpindej/netrock/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SvelteKit](https://img.shields.io/badge/SvelteKit-Svelte_5-FF3E00?logo=svelte&logoColor=white)](https://svelte.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/5rHquRptSh)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/fpindej/netrock)

**[Generator](https://netrock.dev)** · **[Live Demo](https://demo.netrock.dev)** · **[Documentation](#documentation)** · **[Quick Start](#quick-start)** · **[Discord](https://discord.gg/5rHquRptSh)**

</div>

---

## Why NETrock?

Every project starts the same way: authentication, role management, rate limiting, validation, API documentation, Docker setup... You spend weeks on infrastructure before writing a single line of business logic.

**NETrock skips all of that.** It ships a production-grade .NET 10 API with a SvelteKit frontend that goes far beyond boilerplate. Users can log in with Google, GitHub, Discord, Apple, Microsoft, Facebook, LinkedIn, GitLab, Slack, or Twitch - configured by admins from the UI, no redeploy needed. Two-factor authentication with TOTP and recovery codes is built in. The permission system enforces role hierarchy. The admin panel manages users, roles, background jobs, and OAuth providers with AES-256-GCM encrypted credentials. Full audit trail, PII compliance, and rate limiting are part of the foundation.

**For developers**, every convention is documented, the architecture is tested at the layer boundary, and Claude Code skills automate common workflows. **For end users**, the product they interact with has dark mode, i18n, a command palette, responsive design, and security features they expect from a real application.

**Fork it, init it, own it.** After initialization, there is no dependency on "the template." It's your code, your architecture, your product. Every decision is documented so you can understand it, change it, or throw it away.

---

## What You Get

**Backend** - JWT auth with token rotation and reuse detection, TOTP two-factor authentication with recovery codes, OAuth/OIDC external login with 10 providers (admin-configurable from the UI), permission-based authorization with role hierarchy, transactional email with Fluid templates and Hangfire-backed retrying delivery, rate limiting, HybridCache, PostgreSQL with soft delete and audit trails, S3-compatible file storage, Hangfire background jobs, OpenAPI docs, health checks, Result pattern with ProblemDetails. [See full details ->](docs/features.md#backend--net-10--c-13)

**Frontend** - Svelte 5 runes, type-safe API client from OpenAPI, Tailwind CSS 4 with shadcn-svelte component library, Cmd+K command palette with permission-gated navigation, BFF proxy with CSRF protection, i18n with per-feature message files (add locales in one config), dark mode, responsive design with 44px touch targets, admin panel with user/role/job/OAuth provider management. [See full details ->](docs/features.md#frontend--sveltekit--svelte-5)

**Infrastructure** - Aspire AppHost for local development (one command for the full stack with OTEL dashboard and MailPit for email testing), production-ready Dockerfiles, init script for project bootstrapping, GitHub Actions CI with smart path filtering, Claude Code skills for development workflows. [See full details ->](docs/features.md#infrastructure--devops)

**Security** - HttpOnly JWT cookies, refresh token rotation with reuse detection, TOTP 2FA with challenge tokens and recovery codes, OAuth state tokens with TOCTOU protection, AES-256-GCM encrypted provider credentials, PII compliance with server-side masking, security stamp propagation, CSP with nonces, rate limiting, input validation everywhere. [See full details ->](docs/security.md)

---

## Quick Start

> **Use the [NETrock Generator](https://netrock.dev) to pick your features and get a ready-to-run project.** No cloning - just choose what you need, download, and start building.
>
> The manual setup below gives you the full template with all features included.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24+](https://nodejs.org/) (run `corepack enable` for pnpm)
- [Git](https://git-scm.com/)

### 1. Clone & Initialize

```bash
git clone https://github.com/fpindej/netrock.git my-saas
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

The init script will ask for your project name and base port, then rename everything and optionally create the initial migration.

### 2. Launch Everything

```bash
dotnet run --project src/backend/MyProject.AppHost
```

That's it. Aspire starts all infrastructure (PostgreSQL, MinIO) as containers and launches the API and frontend dev server. The Aspire Dashboard URL appears in the console - all service URLs (API docs, pgAdmin, MinIO) are linked from the Dashboard.

| Service | URL |
|---|---|
| **Aspire Dashboard** | Shown in console output |
| **Frontend** | `http://localhost:<BASE_PORT>` (default: `http://localhost:13000`) |
| **MailPit (Email Testing)** | `http://localhost:<BASE_PORT + 8>` |

A Superuser is seeded with credentials you provide during `init.sh` / `init.ps1` (defaults: `superuser@test.com` / `Superuser123!`). Configured in `appsettings.Development.json`.

### 3. Start Building

Add your domain entities, services, and pages - the architecture guides you.

---

## Claude Code Integration

NETrock ships with 20+ native [Claude Code](https://docs.anthropic.com/en/docs/claude-code) skills that automate common development workflows. Type `/` in Claude Code to see all available skills.

| Skill | What it does |
|---|---|
| `/new-feature` | Scaffold a full-stack feature - entity, service, controller, frontend page |
| `/new-endpoint` | Add an API endpoint to an existing feature |
| `/new-entity` | Create a domain entity with EF Core configuration |
| `/new-page` | Create a frontend page with routing, i18n, and navigation |
| `/gen-types` | Regenerate frontend TypeScript types from the OpenAPI spec |
| `/create-pr` | Create a PR with session docs, reviews, and labels |
| `/review-pr` | Review a PR for production-readiness |
| `/review-design` | Review frontend components for UI/UX standards |
| `/create-issue` | Create a GitHub issue with labels |
| `/create-release` | Create a GitHub release with auto-generated notes |

Skills are also loaded automatically when Claude Code plans work - it reads the relevant skill and follows the procedure without you having to invoke it. The project also includes `CLAUDE.md` and `FILEMAP.md` as structured context files, plus 13 specialized agents (`.claude/agents/`), lifecycle hooks (`.claude/hooks/`) for auto-formatting, safety gates, and quality checks, pinned plugins (C#/TypeScript LSP plus official .NET, Aspire, and Svelte skill packs), and a Playwright MCP server (`.mcp.json`) for browser-level verification. See [`.claude/README.md`](.claude/README.md) for how it fits together. No separate onboarding needed.

---

## Documentation

| File | Purpose |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | Hard rules, verification, agent team guide, architecture overview |
| [`.claude/agents/`](.claude/agents/) | Specialized agents - engineers, reviewers, designers, devops, tech writer |
| [`.claude/skills/`](.claude/skills/) | Procedures, convention references, and design tokens (use `/` to list) |
| [`.claude/hooks/`](.claude/hooks/) | Lifecycle hooks - auto-format, safety gates, quality checks |
| [`FILEMAP.md`](FILEMAP.md) | Change impact tables - "when you change X, also update Y" |

Deep dives: **[Features](docs/features.md)** · **[Security](docs/security.md)** · **[Architecture](docs/architecture.md)** · **[Development](docs/development.md)** · **[Before You Ship](docs/before-you-ship.md)** · **[Troubleshooting](docs/troubleshooting.md)**

---

## Localization

i18n with [Paraglide JS](https://inlang.com/m/gerre34r/library-inlang-paraglideJs) - type-safe keys, SSR-compatible, auto-detection via `Accept-Language`. Messages are split into per-feature files (`core`, `auth`, `admin`, etc.) under `messages/{locale}/`. Adding a locale means creating a new directory and translating the feature files you use.

---

## What This Is NOT

NETrock is opinionated by design. It's not:

- **A generic starter** - it makes real choices (PostgreSQL, not "any database"; JWT cookies, not "pluggable auth")
- **A microservices framework** - it's a monolith, because that's what 95% of products should start as
- **A frontend framework** - SvelteKit is included, but you can use just the API with any other frontend
- **Magic** - you still need to understand .NET (and SvelteKit if you keep it)

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Support the Project

NETrock is free and open source under the [MIT License](LICENSE). If it saves you time, consider supporting its development:

<a href="https://buymeacoffee.com/fpindej" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="50"></a>

Star the repo on [GitHub](https://github.com/fpindej/netrock) · Join the [Discord](https://discord.gg/5rHquRptSh) · Need custom development, consulting, or training? [Get in touch](mailto:contact@mail.pindej.cz)

---

## License

This project is licensed under the [MIT License](LICENSE).
