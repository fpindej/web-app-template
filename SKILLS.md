# Skills — Operations Cookbook

Step-by-step recipes for common operations. Each recipe lists exact paths, patterns, and commands. Follow mechanically — no interpretation needed.

> **Breaking change?** Before modifying any existing file, check [FILEMAP.md](FILEMAP.md) for downstream impact. When a change affects public API surface (DTOs, endpoints, interfaces), prefer additive changes over modifications. If a breaking change is unavoidable, document it in the commit body and update all affected consumers in the same commit.

---

## Backend Skills

### Add a NuGet Package

1. Add version to `src/backend/Directory.Packages.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="X.Y.Z" />
   ```
2. Add reference to the target `.csproj` **without** a version:
   ```xml
   <PackageReference Include="PackageName" />
   ```
3. Verify: `dotnet build src/backend/MyProject.slnx`

### Add an Error Message

1. Open `src/backend/MyProject.Domain/ErrorMessages.cs`
2. Add `const string` to the appropriate nested class (or create a new one):
   ```csharp
   public static class Orders
   {
       public const string NotFound = "Order not found.";
   }
   ```
3. Use in service: `Result.Failure(ErrorMessages.Orders.NotFound)`
4. For dynamic messages, use inline interpolation: `$"Order '{orderNumber}' not found."`

### Add an Entity (End-to-End)

**Domain layer:**

1. Create `src/backend/MyProject.Domain/Entities/{Entity}.cs`:
   ```csharp
   public class Order : BaseEntity
   {
       public string Name { get; private set; } = string.Empty;
       protected Order() { }
       public Order(string name) { Id = Guid.NewGuid(); Name = name; }
   }
   ```
2. If enums needed, create alongside entity with explicit integer values
3. Add error messages to `ErrorMessages.cs`

**Application layer:**

4. Create `src/backend/MyProject.Application/Features/{Feature}/I{Feature}Service.cs`
5. Create DTOs in `src/backend/MyProject.Application/Features/{Feature}/Dtos/`:
   - `{Operation}Input.cs` (records)
   - `{Entity}Output.cs` (records)
6. *(Optional)* If custom queries needed: create `src/backend/MyProject.Application/Features/{Feature}/Persistence/I{Feature}Repository.cs` extending `IBaseEntityRepository<T>`

**Infrastructure layer:**

7. Create EF config in `src/backend/MyProject.Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs`:
   - Extend `BaseEntityConfiguration<T>`, override `ConfigureEntity`
   - Mark `internal`
   - Add `.HasComment()` on enum columns
8. Add `DbSet<Entity>` to `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs`
9. Create service in `src/backend/MyProject.Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`:
   - Mark `internal`, use primary constructor
10. *(Optional)* If custom repository: create `src/backend/MyProject.Infrastructure/Features/{Feature}/Persistence/{Feature}Repository.cs` extending `BaseEntityRepository<T>`
11. Create DI extension in `src/backend/MyProject.Infrastructure/Features/{Feature}/Extensions/ServiceCollectionExtensions.cs`:
    - Use C# 13 `extension(IServiceCollection)` syntax

**WebApi layer:**

12. Create controller in `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Controller.cs`:
    - Extend `ApiController` (authenticated) or `ControllerBase` (public)
    - XML docs + `[ProducesResponseType]` on every action
13. Create request/response DTOs in `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/{Operation}/`
14. Create mapper in `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Mapper.cs` (mark `internal`)
15. Create validators co-located with request DTOs
16. Wire DI in `src/backend/MyProject.WebApi/Program.cs`

**Migration:**

17. Run:
    ```bash
    dotnet ef migrations add Add{Entity} \
      --project src/backend/MyProject.Infrastructure \
      --startup-project src/backend/MyProject.WebApi \
      --output-dir Features/Postgres/Migrations
    ```

**Verify:** `dotnet build src/backend/MyProject.slnx`

**Commit strategy:** entity+config+errors → interface+DTOs → service+DI → controller+DTOs+mapper+validators → migration

### Add an Endpoint to an Existing Feature

1. *(If new request/response needed)* Create DTOs in `WebApi/Features/{Feature}/Dtos/{Operation}/`
2. *(If new input/output needed)* Create DTOs in `Application/Features/{Feature}/Dtos/`
3. Add method to `Application/Features/{Feature}/I{Feature}Service.cs`
4. Implement in `Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`
5. Add mapper methods to `WebApi/Features/{Feature}/{Feature}Mapper.cs`
6. Add controller action to `WebApi/Features/{Feature}/{Feature}Controller.cs`:
   - `/// <summary>` + `[ProducesResponseType]` + `CancellationToken`
7. Add validator if needed
8. Verify: `dotnet build src/backend/MyProject.slnx`
9. **After deploying/running:** regenerate frontend types (see [Regenerate API Types](#regenerate-api-types))

> **Breaking change check:** If modifying an existing endpoint's request/response shape, this is a breaking change for the frontend. Either version the endpoint or update the frontend in the same PR.

### Add an Options Class

1. Create in the appropriate layer:
   - Infrastructure: `src/backend/MyProject.Infrastructure/{Area}/Options/{Name}Options.cs`
   - WebApi: `src/backend/MyProject.WebApi/Options/{Name}Options.cs`
2. Structure:
   ```csharp
   /// <summary>
   /// Configuration for {area}. Maps to "{SectionName}" in appsettings.json.
   /// </summary>
   public sealed class {Name}Options
   {
       public const string SectionName = "{Section}";

       /// <summary>
       /// Gets or sets the ...
       /// </summary>
       [Required]
       public string Value { get; init; } = string.Empty;
   }
   ```
3. Add section to `src/backend/MyProject.WebApi/appsettings.json` (and `appsettings.Development.json` if dev differs)
4. Register in DI extension:
   ```csharp
   services.AddOptions<{Name}Options>()
       .BindConfiguration({Name}Options.SectionName)
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```
5. Add env var to `.env.example` if configurable at deploy time

### Run a Migration

```bash
dotnet ef migrations add {MigrationName} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Features/Postgres/Migrations
```

To apply (development — runs automatically on startup, but can be run manually):

```bash
dotnet ef database update \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi
```

### Add a Role

1. Add `public const string` field to `src/backend/MyProject.Application/Identity/Constants/AppRoles.cs`
2. That's it — `AppRoles.All` discovers roles via reflection, seeding picks them up automatically
3. *(Optional)* To seed default permissions for the new role, add entries to `SeedRolePermissionsAsync()` in `src/backend/MyProject.Infrastructure/Persistence/Extensions/ApplicationBuilderExtensions.cs`

### Add a Permission

**Backend:**

1. Add `public const string` field to the appropriate nested class in `src/backend/MyProject.Application/Identity/Constants/AppPermissions.cs`:
   ```csharp
   public static class Orders
   {
       public const string View = "orders.view";
       public const string Manage = "orders.manage";
   }
   ```
   `AppPermissions.All` discovers permissions via reflection — no manual registration needed.
2. Add `[RequirePermission(AppPermissions.Orders.View)]` to the relevant controller actions
3. *(Optional)* Seed the permission for existing roles in `SeedRolePermissionsAsync()` in `src/backend/MyProject.Infrastructure/Persistence/Extensions/ApplicationBuilderExtensions.cs`
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
7. If adding a new admin page: add a per-page guard in `+page.server.ts`:
   ```typescript
   if (!hasPermission(user, Permissions.Orders.View)) throw redirect(303, '/');
   ```
8. If adding a sidebar nav item: add `permission: Permissions.Orders.View` to the nav item in `SidebarNav.svelte` — items are filtered per-permission, not as a group
9. Verify: `cd src/frontend && npm run format && npm run lint && npm run check`

### Add a Background Job

The template uses [Hangfire](https://www.hangfire.io/) for recurring background jobs with PostgreSQL persistence. Jobs implement the `IRecurringJobDefinition` interface and are auto-discovered at startup.

**1. Create the job class** in `src/backend/MyProject.Infrastructure/Features/Jobs/RecurringJobs/{JobName}Job.cs`:

```csharp
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Brief description of what this job does and why.
/// </summary>
internal sealed class MyCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<MyCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "my-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Daily();

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        // Job logic here — each execution gets its own DI scope
        logger.LogInformation("Job completed");
    }
}
```

Key conventions:
- Mark `internal sealed`
- Use primary constructor for DI (scoped services work — each execution gets its own scope)
- Use `TimeProvider` (never `DateTime.UtcNow`)
- Use descriptive `JobId` (kebab-case, e.g. `"expired-token-cleanup"`)
- Use `Hangfire.Cron` helpers: `Cron.Hourly()`, `Cron.Daily()`, `Cron.Weekly()`, etc.

**2. Register in DI** — add two lines to `src/backend/MyProject.Infrastructure/Features/Jobs/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<MyCleanupJob>();
services.AddScoped<IRecurringJobDefinition>(sp => sp.GetRequiredService<MyCleanupJob>());
```

**3. Verify:** `dotnet build src/backend/MyProject.slnx`

That's it — `UseJobScheduling()` discovers all `IRecurringJobDefinition` implementations and registers them with Hangfire automatically.

**Admin UI:** The job will appear in the admin panel at `/admin/jobs` (requires `jobs.view` permission). Users with `jobs.manage` can trigger, pause, resume, delete, and restore jobs. Pause state is persisted to the database (`hangfire.pausedjobs`) and survives restarts. The "Restore Jobs" button re-registers all job definitions without an app restart.

**Configuration:** Job scheduling can be toggled via `appsettings.json`:
```json
"JobScheduling": {
  "Enabled": true,
  "WorkerCount": 4
}
```
Set `Enabled` to `false` to disable Hangfire entirely (e.g. read-only replicas, specific deployment nodes).

**Dev dashboard:** In development, the built-in Hangfire dashboard is available at `http://localhost:8080/hangfire`.

### Fire a One-Time Background Job

For ad-hoc work that should run once in the background (send email, call external API, process file), use Hangfire's `IBackgroundJobClient` directly. No custom interface needed — any DI-registered service with a public method works.

**1. Create the job class** (or use any existing service) in `src/backend/MyProject.Infrastructure/Features/Jobs/`:

```csharp
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs;

internal sealed class WelcomeEmailJob(
    IEmailService emailService,
    ILogger<WelcomeEmailJob> logger)
{
    public async Task ExecuteAsync(string userId, string email)
    {
        await emailService.SendWelcomeAsync(email);
        logger.LogInformation("Sent welcome email to user '{UserId}'", userId);
    }
}
```

Key conventions:
- All parameters must be **JSON-serializable** (strings, numbers, DTOs) — Hangfire persists them to the database
- Never pass `IServiceProvider`, `HttpContext`, `DbContext`, or other non-serializable objects as arguments
- Hangfire creates a fresh DI scope per execution, so scoped services (like `DbContext`) are safe to inject via constructor

**2. Register in DI** — add to `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<WelcomeEmailJob>();
```

**3. Enqueue from any service or controller** — inject `IBackgroundJobClient`:

```csharp
// Fire-and-forget (runs immediately in background)
backgroundJobClient.Enqueue<WelcomeEmailJob>(
    job => job.ExecuteAsync(user.Id, user.Email));

// Delayed (runs after a time span)
backgroundJobClient.Schedule<WelcomeEmailJob>(
    job => job.ExecuteAsync(user.Id, user.Email),
    TimeSpan.FromMinutes(30));
```

**4. Verify:** `dotnet build src/backend/MyProject.slnx`

See `ExampleFireAndForgetJob.cs` in the codebase for a working reference. The Hangfire dashboard and admin UI at `/admin/jobs` show one-time job executions alongside recurring jobs.

---

## Frontend Skills

### Regenerate API Types

**Prerequisite:** Backend must be running (Docker or IDE).

```bash
cd src/frontend && npm run api:generate
```

This updates `src/frontend/src/lib/api/v1.d.ts`. After regenerating:

1. Review changes in `v1.d.ts` for breaking changes
2. Update any affected API calls or type aliases in `src/frontend/src/lib/types/index.ts`
3. Run `cd src/frontend && npm run check` to catch type errors
4. Commit `v1.d.ts` with the changes that caused the regeneration

### Add a Page

1. Create route directory: `src/frontend/src/routes/(app)/{feature}/`
2. Create `+page.svelte`:
   ```svelte
   <script lang="ts">
       import * as m from '$lib/paraglide/messages';
   </script>

   <svelte:head>
       <title>{m.meta_{feature}_title()}</title>
   </svelte:head>
   ```
3. *(If server data needed)* Create `+page.server.ts`:
   ```typescript
   import { createApiClient } from '$lib/api';
   import type { PageServerLoad } from './$types';

   export const load: PageServerLoad = async ({ fetch, url }) => {
       const client = createApiClient(fetch, url.origin);
       const { data } = await client.GET('/api/v1/...');
       return { ... };
   };
   ```
4. Add i18n keys to `src/frontend/src/messages/en.json` and `cs.json`
5. Add navigation entry in `src/frontend/src/lib/components/layout/SidebarNav.svelte`
6. Verify: `cd src/frontend && npm run format && npm run lint && npm run check`

### Add a Component

1. Create feature folder: `src/frontend/src/lib/components/{feature}/`
2. Create component: `{Name}.svelte` with `interface Props` + `$props()`
3. Create barrel: `src/frontend/src/lib/components/{feature}/index.ts`:
   ```typescript
   export { default as {Name} } from './{Name}.svelte';
   ```
4. Import via barrel: `import { {Name} } from '$lib/components/{feature}';`

### Add i18n Keys

1. Add to `src/frontend/src/messages/en.json`:
   ```json
   { "{domain}_{feature}_{element}": "English text" }
   ```
2. Add to `src/frontend/src/messages/cs.json`:
   ```json
   { "{domain}_{feature}_{element}": "Czech text" }
   ```
3. Use: `import * as m from '$lib/paraglide/messages'; m.{domain}_{feature}_{element}()`

### Add a shadcn Component

```bash
cd src/frontend && npx shadcn-svelte@latest add {component-name}
```

Generates in `src/frontend/src/lib/components/ui/{component}/`. After adding:

1. Convert any physical CSS to logical (`ml-*` → `ms-*`, etc.)
2. Available: alert, avatar, badge, button, card, checkbox, dialog, dropdown-menu, input, label, phone-input, select, separator, sheet, sonner, textarea, tooltip
3. Browse full catalog: [ui.shadcn.com](https://ui.shadcn.com)

### Add an npm Package

1. `cd src/frontend && npm install {package}`
2. For dev dependencies: `npm install -D {package}`
3. Verify: `npm run check`

### Style & Responsive Design Pass

Recipe for reviewing or improving a page's styling, responsiveness, and UX.

**1. Audit the page at key viewports:**

Open the page and check at these widths: **320px**, **375px**, **768px**, **1024px**, **1440px**.

**2. Check these rules (see `src/frontend/AGENTS.md` Styling section):**

| Rule | Check |
|---|---|
| Logical CSS only | No `ml-*`/`mr-*`/`pl-*`/`pr-*` — use `ms-*`/`me-*`/`ps-*`/`pe-*` |
| Mobile-first | Base styles for 320px, then `sm:`, `md:`, `lg:`, `xl:` for larger |
| Touch targets | Interactive elements ≥ 40px (`h-10`), primary actions ≥ 44px (`h-11`) |
| Font sizes | Minimum `text-xs` (12px) — never `text-[10px]` or smaller |
| Responsive padding | Scale with breakpoints (`p-4 sm:p-6 lg:p-8`) — no flat large padding |
| Grid in dialogs | Always `grid-cols-1` base with responsive breakpoint for multi-column |
| Sidebar-aware grids | Use `xl:grid-cols-2` for content grids — not `lg:` (sidebar takes ~250px) |
| Full-height layouts | `h-dvh` not `h-screen` (accounts for mobile browser chrome) |
| Flex overflow | `min-w-0` on flex children with text, `truncate`/`overflow-hidden` where needed |
| Non-shrinking elements | `shrink-0` on icons, badges, buttons alongside text |
| Reduced motion | `motion-safe:` prefix on animations, `prefers-reduced-motion` media query |
| No max-width on cards | Cards inside app layout fill their container — no `max-w-2xl` |
| `gap-*` over `space-x-*` | On flex/grid containers, use `gap-*` (direction-agnostic) |

**3. Apply existing page layout patterns:**

| Page type | Layout |
|---|---|
| Info + actions (2-col) | `grid gap-6 xl:grid-cols-2` |
| Single-column forms | `space-y-8` (no max-width) |
| Table + search | Full-width table, search bar above |

**4. Use the design system:**

- Colors: Use CSS variables from `src/frontend/src/styles/themes.css` via Tailwind tokens in `tailwind.css`
- shadcn components: Check [ui.shadcn.com](https://ui.shadcn.com) before building custom UI
- Class merging: Always use `cn()` from `$lib/utils` for conditional classes
- Animations: Define in `src/frontend/src/styles/animations.css`, use `motion-safe:` prefix

**5. Adding a theme variable:**

1. Define in `src/frontend/src/styles/themes.css` (`:root` + `.dark`)
2. Map in `src/frontend/src/styles/tailwind.css` (`@theme inline`)
3. Use in components: `bg-{variable}`, `text-{variable}-foreground`

**6. Verify:**

```bash
cd src/frontend && npm run format && npm run lint && npm run check
```

---

## Full-Stack Skills

### Add an Environment Variable

**Backend-consumed variable:**

1. Add to `.env.example` with a working default value and comment
2. If it maps to an Options class: use `Section__Key` naming (e.g., `Authentication__Jwt__ExpiresInMinutes=100`)
3. If it needs Docker wiring: add to `docker-compose.local.yml` `environment` block with `${VAR}` interpolation
4. If it needs an Options class: follow [Add an Options Class](#add-an-options-class)

**Frontend-consumed variable:**

1. Add to `src/frontend/.env.example`
2. Add to `src/frontend/src/lib/config/server.ts` (server-only) or `i18n.ts` (client-safe)
3. Never export server config from the barrel (`$lib/config/index.ts`)

### Add a Full-Stack Feature

Combines backend entity creation with frontend page. Follow in order:

**Backend (see [Add an Entity](#add-an-entity-end-to-end)):**

1. Domain: entity + enums + error messages
2. Application: interface + DTOs + (optional) repository interface
3. Infrastructure: EF config + DbSet + service + (optional) repository + DI extension
4. WebApi: controller + DTOs + mapper + validators + Program.cs wiring
5. Migration
6. Verify: `dotnet build src/backend/MyProject.slnx`

**Frontend (with backend running):**

7. Regenerate types: `cd src/frontend && npm run api:generate`
8. Add type alias to `src/frontend/src/lib/types/index.ts`
9. Create components in `src/frontend/src/lib/components/{feature}/` with barrel
10. Create page in `src/frontend/src/routes/(app)/{feature}/`
11. Add i18n keys to both `en.json` and `cs.json`
12. Update sidebar navigation
13. Verify: `cd src/frontend && npm run format && npm run lint && npm run check`

**Commit strategy:** backend entity → backend service → backend controller → migration → frontend types+components → frontend page+i18n+nav

---

## Breaking Change Guidelines

When modifying existing code (not creating new), follow these rules:

### What Counts as a Breaking Change

| Layer | Breaking change |
|---|---|
| **Domain entity** | Renaming/removing a property, changing a type |
| **Application interface** | Changing a method signature, renaming/removing a method |
| **Application DTO** | Renaming/removing a field, changing nullability |
| **WebApi endpoint** | Changing route, method, request/response shape, status codes |
| **WebApi response DTO** | Renaming/removing a property, changing type or nullability |
| **Frontend API types** | Always regenerated — broken by any backend DTO change |
| **i18n keys** | Renaming a key (all usages break) |

### Safe Strategies

1. **Additive only** — add new fields/endpoints, never remove or rename existing ones
2. **Same-PR migration** — if a breaking change is needed, update all consumers (including frontend types) in the same PR
3. **V2 endpoint** — for significant endpoint changes, create a new versioned endpoint alongside the old one:
   - New route: `api/v2/{feature}/{action}`
   - Keep `api/v1/` working until all consumers migrate
   - Document deprecation in the commit body
4. **Deprecate then remove** — mark old code as obsolete in one PR, remove in a follow-up after confirming nothing depends on it

### Pre-Modification Checklist

Before changing any existing interface, DTO, or endpoint:

1. Check [FILEMAP.md](FILEMAP.md) for impact
2. Search for all usages: `grep -r "InterfaceName\|MethodName" src/`
3. If the change affects the OpenAPI spec → frontend types are affected → regenerate and fix
4. If the change affects i18n keys → update all `.json` message files and all component usages
5. Document the breaking change in the commit body
