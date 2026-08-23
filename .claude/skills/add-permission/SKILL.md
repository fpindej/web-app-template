---
description: Add a permission constant across backend and frontend
user-invocable: true
---

Adds a permission constant across backend and frontend.

## Steps

**Backend:**

1. Add `public const string` field to the appropriate nested class in `src/backend/MyProject.Application/Identity/Constants/AppPermissions.cs`:
   ```csharp
   public static class Orders
   {
       public const string View = "orders.view";
       public const string Manage = "orders.manage";
   }
   ```
   `AppPermissions.All` discovers permissions via reflection - no manual registration needed.
2. Add `[RequirePermission(AppPermissions.Orders.View)]` to the relevant controller actions
3. *(Optional)* Add the permission to a role's `DefaultPermissions` in `AppRoles.Definitions` (`src/backend/MyProject.Application/Identity/Constants/AppRoles.cs`) - the startup seeder applies it additively
4. Verify: `dotnet build src/backend/MyProject.slnx`

**Frontend:**

5. Add matching constants to `src/frontend/src/lib/utils/permissions.ts`:
   ```typescript
   Orders: {
       View: 'orders.view',
       Manage: 'orders.manage',
   },
   ```
6. Use in components: `hasPermission(user, Permissions.Orders.View)`
7. If adding a new admin page: register it in `adminRoutes` in `$lib/config/routes.ts` (path + permission), then guard in `+page.server.ts`:
   ```typescript
   if (!hasPermission(user, adminRoutes.orders.permission)) throw redirect(303, routes.dashboard);
   ```
8. If adding a sidebar nav item: use `adminRoutes.orders.path` and `.permission` for the nav item in `AppSidebar.svelte` - items are filtered per-permission, not as a group
9. Verify: `cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check`
