# Development Workflow

This document covers git conventions, issue management, pull requests, and session documentation.

## Git Discipline

### Conventional Commits

```
feat(auth): add refresh token rotation
fix(profile): handle null phone number in validation
refactor(persistence): extract pagination into extension method
chore: update NuGet packages
docs: add session notes for orders feature
test(auth): add login integration tests
```

Format: `type(scope): lowercase imperative description`

Rules:
- **Max 72 characters** for the first line
- **No period** at the end
- **Scope is optional** but encouraged — use the feature area (e.g., `auth`, `profile`, `persistence`)
- **Add a body** to explain *why* when the reason isn't obvious from the title

### Atomic Commits

One commit = one logical change that could be reverted independently.

| Good (Atomic) | Bad (Bundled) |
|---|---|
| `feat(orders): add Order entity and EF config` | `feat: add entire orders feature` |
| `feat(orders): add IOrderService and DTOs` | (entity + service + controller + frontend |
| `feat(orders): implement OrderService` | all in one massive commit) |
| `feat(orders): add OrdersController with endpoints` | |
| `feat(orders): add order list page in frontend` | |

**Commit continuously.** Every logically complete unit of work gets its own commit immediately — don't accumulate changes and commit at the end.

### Pre-Commit Checks

Before **every** commit, verify the code compiles and passes checks:

- **Backend**: `dotnet build src/backend/MyProject.slnx`
- **Frontend**: `cd src/frontend && npm run format && npm run lint && npm run check`

Never commit code that doesn't compile, has lint errors, or fails type checks.

### Branch Hygiene

Work on the current branch unless instructed otherwise. For new branches:

- `feat/{name}` — new features
- `fix/{description}` — bug fixes

---

## GitHub Issues

### Creating Issues

Use `gh issue create` with:

- **Title**: Conventional Commit format (`type(scope): description`)
- **Body**: Problem description, proposed fix, and affected files
- **Labels**: Apply all relevant labels (see Labels section)

### Breaking Down Large Issues

When an issue spans multiple layers (backend + frontend), involves multiple logical steps, or could be worked on by different developers in parallel, break it into **sub-issues**.

| Signal | Example |
|---|---|
| Crosses stack boundary | Backend endpoint + frontend page → separate issues |
| Independent deliverables | Database migration + service + controller could each be reviewed alone |
| Multiple logical concerns | New entity + new API + new UI page + new i18n keys |
| Parallelizable work | Two developers could work on different sub-issues simultaneously |

**When NOT to split:**
- Small, tightly coupled changes that only make sense together (e.g., adding a DTO and its validator)
- Single-layer fixes that take one commit (e.g., fixing a typo, adding an index)

### Sub-Issues API

Use the GitHub Sub-Issues API (not markdown checkboxes) for native progress tracking:

```bash
# 1. Create parent issue
gh issue create --title "feat(auth): add change password endpoint" \
  --body "..." --label "backend,frontend,feature"

# 2. Create sub-issues
gh issue create --title "feat(auth): add change password endpoint (backend)" \
  --body "..." --label "backend,feature"

# 3. Get sub-issue's numeric ID
gh api --method GET /repos/{owner}/{repo}/issues/{sub_issue_number} --jq '.id'

# 4. Link to parent
gh api --method POST /repos/{owner}/{repo}/issues/{parent_number}/sub_issues \
  --field sub_issue_id={sub_issue_id}
```

Each sub-issue gets its own branch, PR, and labels. The parent issue is closed when all sub-issues are done.

---

## Pull Requests

Use `gh pr create` with:

- **Title**: Conventional Commit format matching the branch scope
- **Body**: Summary of changes, linked issues if applicable
- **Base**: `master` (unless instructed otherwise)
- **Labels**: Apply all relevant labels

PRs are created only when explicitly requested.

---

## Labels

Apply **all** that fit — they are not mutually exclusive.

| Label | Color | Description | Use When |
|---|---|---|---|
| `backend` | `#0E8A16` | Backend (.NET) | Changes touch `src/backend/` |
| `frontend` | `#1D76DB` | Frontend (SvelteKit) | Changes touch `src/frontend/` |
| `security` | `#D93F0B` | Security-related | Fixes vulnerabilities, hardens config, adds auth features |
| `feature` | `#5319E7` | New feature or enhancement | Adding new capabilities |
| `bug` | `#d73a4a` | Something isn't working | Fixing incorrect behavior |
| `documentation` | `#0075ca` | Documentation | Changes to docs, AGENTS.md, session notes |

If a new label would genuinely help categorize work and none of the existing ones cover it, create it with `gh label create` before applying.

---

## Session Documentation

When asked to wrap up or create session docs, generate a documentation file:

- **Location**: `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md`
- **Template**: See [`docs/sessions/README.md`](sessions/README.md) for the required structure
- **Commit**: As the final commit of the session: `docs: add session notes for {topic}`

Session docs are created **only when explicitly requested**, never automatically.

### When to Include Mermaid Diagrams

| Diagram Type | Use For |
|---|---|
| `flowchart TD` | Request/data flows, layer interactions |
| `erDiagram` | Entity relationships |
| `sequenceDiagram` | Multi-step flows (auth, token refresh) |
| `classDiagram` | Service/interface relationships |
| `stateDiagram-v2` | State transitions (order lifecycle) |

Keep diagrams focused — one concern per diagram.
