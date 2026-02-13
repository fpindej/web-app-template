Create a GitHub issue with proper labels and optional sub-issues.

Ask the user for:
1. **What needs to be done** (or a title if they have one)
2. **Type** — `feat`, `fix`, `refactor`, `chore`, `docs`
3. **Scope** — which area (e.g., `auth`, `orders`, `frontend`)

## Execution

Follow the conventions in **AGENTS.md → "Issues"** section.

**Title format:** `type(scope): lowercase imperative description`

**Labels** — apply all that fit: `backend`, `frontend`, `security`, `feature`, `bug`, `documentation`

**Split into sub-issues when:**
- Crosses stack boundary (backend + frontend)
- Multiple independent deliverables
- Parallelizable work

**Link sub-issues** using the GitHub Sub-Issues API (see AGENTS.md for exact commands).

Report the created issue URL(s) to the user.
