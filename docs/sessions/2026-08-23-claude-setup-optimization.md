# Claude Code Setup Optimization

**Date**: 2026-08-23
**Scope**: Full audit and optimization of the `.claude/` agentic coding setup: agents, skills, rules, hooks, settings, plugins, and MCP.

## Summary

Audited the entire `.claude/` setup (13 agents, 30 skills, 5 rules, 4 hooks, settings) with parallel deep-audit agents plus a docs-verified best-practices check, then cross-checked against an independent external audit artifact. The setup was already strong and repo-specific; the work fixed contradictions (commit ownership, v1.d.ts rule, touch targets 40 vs 44px), removed ~500 lines of duplicated checklists, made the hooks actually enforce what they claimed (real Stop gate, correct timeout units), and adopted modern harness features: path-scoped rules, pinned LSP plugins, vendor skill-pack marketplaces (Microsoft .NET/Aspire, Svelte), and a checked-in Playwright MCP server.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `.claude/settings.json` | `attribution` object, `enabledPlugins` (csharp-lsp, typescript-lsp, dotnet-*, aspire, svelte), `extraKnownMarketplaces`, hook timeouts 15000/30000 -> 15/30, SessionStart matcher `startup` | Enforce no-attribution in config not prose; pin tooling for cloners; timeouts are in seconds (old values meant ~4h/8h); skip env check on resume/compact |
| `.claude/hooks/stop-quality-gate.mjs` | Rewritten: blocks completion once per dirty-file set (session marker + `stop_hook_active` fallback) | Old version only emitted `systemMessage`, which Claude never sees - it gated nothing |
| `.claude/hooks/auto-format.mjs` | Scope formatting to `src/backend` / `src/frontend` | Frontend prettier was reformatting backend and config JSON |
| `.claude/hooks/session-start.mjs` | 5s timeouts per check; local `dotnet tool list` | Stopped Docker could hang startup; repo uses `.config/dotnet-tools.json`, not global tools |
| `.claude/hooks/validate-bash.mjs` | Block `git push --delete` | Only the `:branch` refspec form was covered |
| `.claude/agents/*` (engineers) | No longer commit; report suggested commit messages | CLAUDE.md gives the orchestrator commit ownership; two writers caused competing commits |
| `.claude/agents/backend-reviewer.md`, `frontend-reviewer.md`, `ux-designer.md` | Slimmed to role + priorities + output format (~240 lines cut) | Bodies duplicated the convention skills they already preload; drift risk (40 vs 44px proved it) |
| `.claude/agents/devops-reviewer.md`, `filemap-checker.md` | Dropped Bash / added scoped git commands | Tool lists contradicted their instructions |
| `.claude/agents/devops-engineer.md` | Removed stale `deploy/`, fixed Dockerfile path, .env-only credential pinning | Stale references; committed credentials conflicted with infra rules |
| `.claude/agents/tech-writer.md`, `product-owner.md`, `test-writer.md` | Trimmed to project-specific facts; scope clarified vs orchestrator/engineers | Generic advice the model already follows; delegation-rule contradiction |
| `.claude/rules/*.md` | Added `paths:` frontmatter | Rules now load on demand instead of always occupying orchestrator context |
| `.claude/skills/gen-types` | Removed hand-edit fallback for `v1.d.ts` | Directly contradicted a CLAUDE.md hard rule |
| `.claude/skills/create-pr`, `create-release`, `review-design` | Signing per git config; default-branch detection instead of hardcoded `master` | `-S` fails on fresh clones; generated projects may use `main` |
| `.claude/skills/review-pr` | Runs inline, dispatches reviewer agents in parallel; deleted `conventions-summary.md` | `context: fork` + `agent: Explore` could not spawn the reviewers CLAUDE.md promises; reference was a 4th copy of the Hard Rules |
| `.claude/skills/review-dependabot` | Tests PR branches in a disposable worktree | `gh pr checkout` silently switched the user's branch |
| `.claude/skills/add-permission` | adminRoutes registry pattern for guards/nav | Steps predated the routes registry refactor |
| `.claude/skills/frontend-conventions` | Generic folder purposes + barrel-listing instruction; added `dashboard/`, `oauth/`; 44px touch targets | Literal component inventory was already drifting; 40px contradicted the hard rule |
| `.claude/skills/add-aspire-dep`, `new-page`, `new-endpoint` | Un-hardcoded Aspire version; fixed step numbering; route-constraint pointer | Accuracy fixes |
| `.claude/skills/add-route-constraint/` | Deleted | Trivial pattern with three live examples in `WebApi/Routing/` |
| `.claudeignore` | Deleted | Not a Claude Code mechanism; dead config |
| `.mcp.json` | Added (Playwright MCP) | Browser-level verification of frontend changes |
| `.claude/README.md` | Added | Explains how agents/skills/rules/hooks/plugins fit together for template consumers |
| `CLAUDE.md` | Commit ownership stated; orchestrator-vs-tech-writer and CI/CD scope conflicts resolved; File Roles extended | Contradiction fixes |

## Decisions & Reasoning

### Commit ownership: orchestrator commits, subagents never do

- **Choice**: Engineers implement and verify; the orchestrator commits after reviewers pass, using engineer-suggested messages.
- **Alternatives considered**: Engineers commit their own work (old fullstack-engineer flow).
- **Reasoning**: CLAUDE.md already assigned git operations to the orchestrator; two writers produced competing commit instructions. The rewritten Stop gate enforces the same policy.

### Convention checklists live only in the `*-conventions` skills

- **Choice**: Reviewer agents carry role/priorities/output format; all rules come from the preloaded skill.
- **Alternatives considered**: Keep inline checklists as a hedge.
- **Reasoning**: The same rules existed in up to four places and had already diverged (touch targets, logical CSS lists). One source of truth eliminates drift; the `skills:` preload is verified to work.

### Reviewer roster kept at 13 agents (external audit proposed 7)

- **Choice**: Keep product-owner, test-writer, tech-writer, devops-engineer, and a separate ux-designer, with clarified scope boundaries.
- **Alternatives considered**: Merge ux-designer into frontend-reviewer, fold devops-engineer into backend-engineer, drop the rest.
- **Reasoning**: The cost argument targeted the duplicated checklists, which are now gone; remaining cost is one description line each. Parallel review lenses and infra/backend separation are worth that. Revisit if netrock-cli template sync burden grows.

### Plugin defaults: LSPs + vendor skill packs, no heavy always-on plugins

- **Choice**: Pin csharp-lsp, typescript-lsp, dotnet-aspnetcore/-data/-test, aspire, svelte. Explicitly not superpowers, pr-review-toolkit, or security-guidance.
- **Alternatives considered**: security-guidance was briefly enabled.
- **Reasoning**: security-guidance builds a Python venv on SessionStart and LLM-reviews every commit/push/stop - wrong default for a template. Vendor packs are official (Microsoft, Svelte), lazy, and users can opt out via `settings.local.json`.

## Follow-Up Items

- [ ] netrock-cli templates: sync the mirrored `.claude/` copy (20-file drift), remove the dangling `frontend-conventions` preload in its test-writer, ship frontend agents when the frontend feature is enabled, and inherit the default-branch fixes
- [ ] Optionally collapse CLAUDE.md's three overlapping delegation tables (~150 -> ~80 lines)
- [ ] Consider trimming FILEMAP.md's Naming Patterns section (~90 lines duplicating backend-conventions)
- [ ] Vendor marketplaces require a one-time `/plugin install` per user; consider printing the commands from `init.sh`
