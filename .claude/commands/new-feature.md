Create a full-stack feature: backend entity through to frontend page.

Ask the user for:
1. **Feature name** (e.g., `Orders`)
2. **Entity properties** (name, type, nullability, enums)
3. **Endpoints needed** (CRUD or custom — methods, routes, request/response shapes)
4. **Frontend page details** (route, components, data display)
5. **Authorization** (public, authenticated, role-based?)

## Execution

Follow **SKILLS.md → "Add a Full-Stack Feature"**, which chains these recipes:

1. Backend: "Add an Entity" (all layers) → verify build
2. Migration → verify build
3. Frontend: "Regenerate API Types" → "Add a Component" → "Add a Page"
4. "Style & Responsive Design Pass" on the new page

Read `src/backend/AGENTS.md` and `src/frontend/AGENTS.md` for layer-specific conventions.

Check **FILEMAP.md** impact tables if the feature touches any existing entities or endpoints.

Commit strategy (atomic, one per logical unit):
1. `feat({feature}): add {Entity} entity and EF configuration`
2. `feat({feature}): add I{Feature}Service and DTOs`
3. `feat({feature}): implement {Feature}Service`
4. `feat({feature}): add {Feature}Controller with endpoints`
5. `feat({feature}): add {Entity} migration`
6. `feat({feature}): add {feature} page in frontend`

Verification checklist:
- `dotnet build src/backend/MyProject.slnx` passes
- `cd src/frontend && npm run format && npm run lint && npm run check` passes
- Migration creates expected tables/columns
- Navigation entry appears in sidebar
- i18n keys present in both language files
