---
description: Regenerate frontend API types from the backend OpenAPI spec
user-invocable: true
---

Regenerates frontend API types from the backend OpenAPI spec.

**Recently changed backend files:**
!`git diff --name-only HEAD~3 -- src/backend/ 2>/dev/null | grep -E '\.(cs)$' | head -20 || echo "(no recent backend changes)"`

## Steps

1. Try generating types:
   ```bash
   cd src/frontend && pnpm run api:generate
   ```

2. **If generation fails**: fix the cause, never the output - `v1.d.ts` is generated and must never be hand-edited (CLAUDE.md hard rule). Typical causes: backend does not build (`dotnet build src/backend/MyProject.slnx`), or the OpenAPI spec export step failed - read the generator error. If you cannot fix the cause, stop and report the blocker instead of shipping hand-written types.

3. Check what changed - look for renamed/removed schemas (breaking) vs added schemas (safe)

4. Update type aliases in `src/frontend/src/lib/types/index.ts` if schemas changed

5. Fix type errors:
   ```bash
   cd src/frontend && pnpm run check
   ```
   If errors: the backend made a breaking API change - fix all frontend consumers

6. Format: `cd src/frontend && pnpm run format`

7. Commit `v1.d.ts` with the backend changes that caused the regeneration
