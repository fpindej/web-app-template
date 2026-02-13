Add an API endpoint to an existing feature.

Ask the user for:
1. **Feature name** (existing feature, e.g., `Authentication`, `Admin`, `Users`)
2. **Operation** (e.g., `GetOrders`, `UpdateStatus`)
3. **HTTP method and route** (e.g., `GET /api/v1/orders/{id}`)
4. **Request shape** (if POST/PUT/PATCH — fields with types)
5. **Response shape** (fields with types)
6. **Authorization** (public, authenticated, role-based?)

## Execution

Follow **SKILLS.md → "Add an Endpoint to an Existing Feature"**.

Read `src/backend/AGENTS.md` for conventions on controllers, DTOs, mappers, validators, and OpenAPI annotations.

**Breaking change check:** If modifying an existing endpoint's request/response shape, check FILEMAP.md impact tables. Either version the endpoint (v2) or update the frontend in the same PR.

After building successfully, regenerate frontend types if the backend is running:
```bash
cd src/frontend && npm run api:generate
```

Commit: `feat({feature}): add {operation} endpoint`
