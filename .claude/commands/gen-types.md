Regenerate frontend API types from the backend OpenAPI spec.

**Prerequisite:** Backend must be running. Verify with:
```bash
curl -s http://localhost:8080/openapi/v1.json | head -1
```

If not running, start with `docker compose -f docker-compose.local.yml up -d api` and wait for it.

## Execution

Follow **SKILLS.md → "Regenerate API Types"**.

1. Run: `cd src/frontend && npm run api:generate`
2. Review what changed in `v1.d.ts` — new schemas (safe) vs modified/removed schemas (breaking)
3. Update type aliases in `src/frontend/src/lib/types/index.ts` if needed
4. Fix any type errors: `cd src/frontend && npm run check`
5. Format: `cd src/frontend && npm run format`

If `npm run check` reveals type errors, the backend made a breaking API change. Fix all consumers before committing.

Commit `v1.d.ts` with the backend changes that caused the regeneration (same commit or same PR).
