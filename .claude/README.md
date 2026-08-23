# Claude Code Setup

How the agentic tooling in this template fits together. Read this once after cloning.

## First Run

1. Open Claude Code in the repo root and accept the workspace trust dialog.
2. The project pins official LSP plugins (`csharp-lsp`, `typescript-lsp`) plus vendor skill packs from Microsoft and Svelte (`dotnet-aspnetcore`/`dotnet-data`/`dotnet-test`, `aspire`, `svelte` - registered via `extraKnownMarketplaces`). Official-marketplace plugins prompt once; for the vendor marketplaces run `/plugin install <name>@<marketplace>` when prompted. Everything works without them - the convention skills are local.
   - Trust note: these marketplaces track their upstream repos (dotnet/skills, microsoft/aspire-skills, sveltejs/ai-tools), so their content can change over time and their skills inject instructions into your agent sessions. They are first-party vendor repos, but review what you install and opt out anytime via `"<plugin>@<marketplace>": false` in `settings.local.json`.
3. Approve the project MCP server from `.mcp.json` (Playwright, used for browser-level verification of frontend changes). The `svelte` plugin adds the official Svelte MCP + LSP for `.svelte` files.
4. Optional: copy `settings.local.json.example` to `settings.local.json` for personal permission overrides (gitignored).

The SessionStart hook checks your prerequisites (.NET SDK, pnpm, dotnet-ef, Docker) and prints what is missing.

## How the Pieces Fit

| Piece | Loaded | Purpose |
|---|---|---|
| `CLAUDE.md` | Always | Hard rules, delegation model, verification commands |
| `rules/*.md` | When matching files are touched (`paths:` frontmatter) | Implementation conventions for the main session |
| `agents/*.md` | On delegation | Engineers implement, reviewers audit read-only |
| `skills/*-conventions` | Injected into agents via `skills:` frontmatter | Full convention references (single source of truth for reviewers) |
| `skills/*` (other) | Via `/name` or automatically | Repeatable procedures (new entity, new endpoint, create PR, ...) |
| `hooks/*.mjs` | Lifecycle events (see `settings.json`) | Guardrails and automation |

## Division of Labor

- The main session is an **orchestrator**: it delegates application code in `src/` to engineer agents, runs reviewer agents in parallel afterwards, and owns all commits. Subagents never commit; they report suggested commit messages.
- Convention checklists live in the `*-conventions` skills, not in agent bodies. Change a convention in one place and every agent that preloads the skill picks it up.

## Hooks

| Hook | Event | What it does |
|---|---|---|
| `session-start.mjs` | SessionStart (startup only) | Prerequisite check: .NET, pnpm, dotnet-ef, Docker |
| `validate-bash.mjs` | PreToolUse (Bash) | Blocks destructive commands (force push, bare reset --hard, curl-pipe-sh, ...) |
| `auto-format.mjs` | PostToolUse (Write/Edit, async) | dotnet format for backend .cs, prettier for frontend files |
| `stop-quality-gate.mjs` | Stop | Blocks finishing once per dirty-file set if `src/` changes are uncommitted; reminds about feature branches |

The stop gate blocks at most once for the same set of dirty files, so it nags exactly once and never loops. Deleting its marker in your temp dir re-arms it.

## Permissions

`settings.json` allowlists the routine toolchain and denies destructive operations, following deny-by-default:

- **docker**: only `build`, `compose`, `info`, `ps`, `images`, `logs`, and `volume ls` are pre-approved. `docker run` prompts, and privileged containers or mounts of `/` and `~` are hard-denied (host-filesystem escape).
- **gh api**: only explicit `--method GET` reads plus two narrow POST endpoints (issue sub-issues, PR comment replies) are pre-approved. Other mutations prompt; `secrets`/`keys`/`hooks` API paths are hard-denied (credential and webhook surface).
- **secrets**: `.env`/`.env.local` are denied for Read as well as Write/Edit, along with `*.pem`/`*.key` reads. `.env.example` and `.env.test` stay accessible. `appsettings.Development.json` is deliberately NOT denied - it is tracked template config with no real secrets, and skills edit it.

The deny list intentionally overlaps with `validate-bash.mjs` - permissions are the hard gate, the hook adds clearer messages and patterns permissions cannot express. Put personal additions in `settings.local.json`, not here.
