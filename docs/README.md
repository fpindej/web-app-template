# Documentation

This directory contains the development guidelines and standards for the project. Start here, then dive into the specific area you need.

## Documentation Map

| Document | What It Covers | Read When |
|---|---|---|
| [Architecture](architecture.md) | System overview, layer responsibilities, data flow diagrams | Understanding how the system fits together |
| [Getting Started](getting-started.md) | Setup, dev workflows, environment config, phone testing | Setting up a dev environment or onboarding |
| [Backend Conventions](backend-conventions.md) | .NET patterns, entities, services, persistence, options | Working in `src/backend/` |
| [Frontend Conventions](frontend-conventions.md) | SvelteKit patterns, components, styling, state, i18n | Working in `src/frontend/` |
| [Security](security.md) | Principles, headers, CSP, CSRF, auth architecture, cookies | Any security-related work or review |
| [API Contract](api-contract.md) | OpenAPI spec, type generation, DTO design, enum handling | Adding/modifying API endpoints or DTOs |
| [Workflow](workflow.md) | Git conventions, commits, PRs, issues, labels, session docs | Day-to-day development workflow |

## For AI Agents

The `AGENTS.md` files are the AI-optimized counterpart to these docs:

| File | Purpose |
|---|---|
| [`AGENTS.md`](../AGENTS.md) | Project overview, quick-reference rules, cross-layer concerns |
| [`src/backend/AGENTS.md`](../src/backend/AGENTS.md) | Backend code patterns, checklists, do/don't rules |
| [`src/frontend/AGENTS.md`](../src/frontend/AGENTS.md) | Frontend code patterns, checklists, do/don't rules |

**The split:** AGENTS.md files contain concise rules and code templates for AI execution. The docs/ files contain explanations, rationale, and design decisions for human understanding. When both exist for a topic, the docs/ file explains *why*; the AGENTS.md file shows *what* and *how*.

## Session Notes

Session-specific documentation (decisions, changes, diagrams from individual work sessions) lives in [`sessions/`](sessions/README.md).
