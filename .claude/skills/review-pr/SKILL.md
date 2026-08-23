---
description: Reviews a pull request for production-readiness. Use when reviewing PRs, checking code changes before merge, or when asked to evaluate a PR.
argument-hint: "[PR number or URL]"
---

Reviews a pull request for production-readiness before merge. Runs in the main session so the reviewer agents can be dispatched in parallel (per CLAUDE.md delegation patterns).

Argument: PR number or URL. If omitted, reviews the current branch's open PR.

**Current branch:** !`git branch --show-current`
**Open PR for this branch:** !`gh pr view --json number,title,url --jq '"#\(.number) \(.title) - \(.url)"' 2>/dev/null || echo "(no open PR)"`

## Steps

1. Resolve the PR: `gh pr view {number} --json number,title,headRefName,body`
2. Get the diff and changed files: `gh pr diff {number}`, `gh pr view {number} --json files`
3. Dispatch reviewer agents **in parallel** (one message, multiple Agent calls), scoped by what changed:
   - `backend-reviewer` - if `src/backend/` changed
   - `frontend-reviewer` - if `src/frontend/` changed
   - `security-reviewer` - always
   - `ux-designer` - if UI components or pages changed
   Give each the PR number and the changed-file list for its scope. They preload the convention skills - do not restate conventions in the prompt.
4. While reviewers run, perform the cross-cutting checks below yourself.

## Orchestrator Checks (cross-cutting)

- **Intent**: does the code do what the PR description says?
- **Cross-stack consistency**: backend DTO changes -> types regenerated (`v1.d.ts`), frontend consumers updated
- **i18n**: new keys present in ALL locale directories
- **Dockerfile**: new `.csproj` referenced by WebApi has a COPY line in the restore layer
- **Completeness**: new flags/props consumed; no dead code introduced
- **No em dashes (U+2014), no emojis** anywhere in the diff
- **Tests**: behavior changes come with test changes

## Verdict Synthesis

Merge the reviewer reports and your own checks into one report:

- **PASS** - what looks good (brief, no padding)
- **FAIL** - issues that MUST be fixed before merge (with file path and line, attributed to the reviewer that found them)
- **WARN** - suggestions, not blockers

Final verdict: `REQUEST CHANGES` if any reviewer reported FAIL/CRITICAL/HIGH items or your checks found blockers; `APPROVE WITH SUGGESTIONS` if only WARN items; otherwise `APPROVE`.

## Rules

- Research only - do NOT modify any files
- Read actual source files when a finding needs context - not just the diff
- Be thorough but not pedantic - flag real issues, not style nitpicks already handled by linters
- For a quick generic pass without project reviewers, the built-in `/code-review` is also available; this skill is the convention-aware review
