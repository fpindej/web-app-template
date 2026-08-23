---
description: Create a frontend page with routing, i18n, and navigation
user-invocable: true
---

Creates a new frontend page with routing, i18n, and navigation.

Infers route, route group, components needed, and data requirements from context. Defaults to `(app)` (authenticated). Asks only if genuinely ambiguous.

## Templates

Use these as starting points - fill in the specifics from context:
- [Page and component templates](assets/page.svelte.md)

## Conventions

- **Button layout**: All action/submit buttons use `w-full sm:w-auto` with wrapper `flex flex-col gap-2 sm:flex-row sm:justify-end`. Default size only (no `size="sm"` or `size="lg"`).

## Steps

**Components (if needed):**

1. Create feature folder: `src/frontend/src/lib/components/{feature}/`
2. Create components with `interface Props` + `$props()`
3. Create barrel `index.ts` exporting all components

**Page:**

4. Create route directory: `src/frontend/src/routes/(app)/{feature}/`
   - Or `(public)/{feature}/` for unauthenticated pages
5. Create `+page.svelte` with `<svelte:head>` using i18n title
6. If server data needed: create `+page.server.ts` using `createApiClient(fetch, url.origin)`
7. If admin page: add entry to `adminRoutes` in `$lib/config/routes.ts` with path and permission
8. If permission-guarded: add check in `+page.server.ts`:
   ```typescript
   if (!hasPermission(user, adminRoutes.feature.permission)) throw redirect(303, routes.dashboard);
   ```

**Integration:**

9. Add i18n keys to the correct feature file in all locale directories
10. Add navigation entry in `AppSidebar.svelte` (using `adminRoutes.feature.path` and `.permission` for admin pages)
11. Add matching entry in `CommandPalette.svelte` (using `adminRoutes.feature.path` and `.permission` for admin pages)
12. If public page: add route to `publicRoutes` in `src/frontend/src/routes/sitemap.xml/+server.ts`

**Verify and commit:**

13. `cd src/frontend && pnpm run format && pnpm run lint && pnpm run check` - fix errors, loop until green
14. Commit: `feat({feature}): add {feature} page`

Paraglide module errors (~32) are expected at check time - ignore those. Fix everything else.
