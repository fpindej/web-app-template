Create a new frontend page with routing, i18n, and navigation.

Ask the user for:
1. **Page name/route** (e.g., `orders`, `admin/reports`)
2. **Route group** — `(app)` for authenticated (default), `(public)` for unauthenticated
3. **Does it need server-side data?** (API calls on load)
4. **Components needed** (list of UI elements on the page)

## Execution

Follow **SKILLS.md** — combine these recipes in order:
1. "Add a Component" — for each component needed
2. "Add a Page" — route, server load, i18n, navigation
3. "Style & Responsive Design Pass" — verify responsive behavior

Read `src/frontend/AGENTS.md` for conventions on Svelte 5 patterns, component organization, and styling rules.

Verify: `cd src/frontend && npm run format && npm run lint && npm run check`

Commit strategy (atomic):
1. `feat({feature}): add {feature} components` — components + barrel
2. `feat({feature}): add {feature} page` — route + server load + i18n + nav
