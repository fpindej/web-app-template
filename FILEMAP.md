# File Map — Change Impact Reference

Quick-reference for "when you change X, also update Y" and "where does X live?"

> **Rule:** Before modifying any existing file listed here, trace its impact row. If a change affects downstream files, update them in the same commit (or same PR at minimum).

---

## Change Impact Tables

### Backend Changes

| When you change... | Also update... |
|---|---|
| **Domain entity** (add/rename property) | EF configuration, migration, Application DTOs, WebApi DTOs, mapper, frontend types (`npm run api:generate`) |
| **Domain entity** (add enum property) | EF config (`.HasComment()`), `EnumSchemaTransformer` handles the rest automatically |
| **`ErrorMessages.cs`** (add/rename constant) | Service that uses it; frontend may display message directly |
| **`Result.cs`** (change pattern) | Every service + every controller that matches on `Result` |
| **Application interface** (change signature) | Infrastructure service implementation, controller calling the service |
| **Application DTO** (add/rename/remove field) | Infrastructure service, WebApi mapper, WebApi request/response DTO, frontend types |
| **Infrastructure EF config** (change mapping) | Run new migration |
| **`MyProjectDbContext`** (add DbSet) | Run new migration |
| **Infrastructure service** (change behavior) | Verify controller still maps correctly, verify error messages still apply |
| **Infrastructure Options class** | `appsettings.json`, `appsettings.Development.json`, `.env.example`, DI registration |
| **DI extension** (new service registration) | `Program.cs` must call the extension |
| **WebApi controller** (change route/method) | Frontend API calls, `v1.d.ts` regeneration |
| **WebApi request DTO** (add/rename/remove property) | Validator, mapper, frontend types, frontend form |
| **WebApi response DTO** (add/rename/remove property) | Mapper, frontend types, frontend component displaying data |
| **WebApi validator** (change rules) | Consider matching frontend validation UX |
| **`Program.cs`** (change middleware order) | Test full request pipeline — order matters for auth, CORS, rate limiting |
| **`Directory.Packages.props`** (change version) | `dotnet build` to verify compatibility |
| **`Directory.Build.props`** (change TFM/settings) | All projects in solution |
| **`BaseEntity.cs`** | `BaseEntityConfiguration`, `AuditingInterceptor`, all entities |
| **`BaseEntityConfiguration.cs`** | All entity configurations that extend it |
| **`AppRoles.cs`** (add role) | Role seeding picks up automatically; add `[Authorize(Roles = "...")]` where needed |
| **OpenAPI transformers** | Regenerate frontend types to verify; check Scalar UI |

### Frontend Changes

| When you change... | Also update... |
|---|---|
| **`v1.d.ts`** (regenerated) | Type aliases in `$lib/types/index.ts`, any component using changed schemas |
| **`$lib/types/index.ts`** (add/rename alias) | All imports of the changed type |
| **`$lib/api/client.ts`** | Every component using `browserClient` or `createApiClient` |
| **`$lib/api/error-handling.ts`** | Components that call `getErrorMessage`, `mapFieldErrors`, `isValidationProblemDetails` |
| **`$lib/config/server.ts`** | Server load functions that import `SERVER_CONFIG` |
| **`$lib/config/i18n.ts`** | `LanguageSelector`, root layout |
| **`hooks.server.ts`** | All server responses (security headers, locale) |
| **`svelte.config.js`** (CSP) | Test that scripts/styles/images still load |
| **`app.html`** | FOUC prevention, nonce attribute, theme init |
| **Component barrel `index.ts`** | All imports from that feature folder |
| **i18n keys** (rename/remove in `en.json`) | Same key in `cs.json`, all `m.{key}()` usages |
| **i18n keys** (add) | Add to both `en.json` and `cs.json` |
| **Layout components** (Sidebar, Header) | All pages that use the app shell |
| **`SidebarNav.svelte`** | Navigation links for all pages |
| **Route `+layout.server.ts`** | All child routes that depend on parent data |
| **Route `+page.server.ts`** | The corresponding `+page.svelte` |
| **Styles (`themes.css`)** | `tailwind.css` mappings, components using the variables |
| **Styles (`tailwind.css`)** | Components using custom Tailwind tokens |
| **`components.json`** (shadcn config) | Future `npx shadcn-svelte@next add` commands |
| **`package.json`** (scripts) | CI/CD references, CLAUDE.md pre-commit checks |

### Cross-Stack Changes

| When you change... | Also update... |
|---|---|
| **Backend endpoint route** | Frontend API calls + regenerate types |
| **Backend response shape** | Regenerate types → update frontend components |
| **Backend auth/cookie behavior** | Frontend `$lib/api/client.ts` (refresh logic), `$lib/auth/auth.ts` |
| **`.env.example`** | `docker-compose.local.yml` if variable needs Docker wiring |
| **`docker-compose.local.yml`** | `.env.example` if new variable introduced |
| **CORS config** (`CorsExtensions.cs`) | Frontend dev server origin, `ALLOWED_ORIGINS` env var |
| **Rate limiting config** | Frontend may need retry/backoff logic |
| **`appsettings.json`** structure | Options class, `.env.example`, `docker-compose.local.yml` |
| **Security headers** (backend or frontend) | Verify both sides are consistent |

---

## Key Files Quick Reference

Files that are frequently referenced in impact tables above. For anything not listed here, use Glob/Grep — the codebase follows predictable naming patterns documented in the AGENTS.md files.

### Backend Naming Patterns

```
src/backend/MyProject.{Layer}/
  Domain:          Entities/{Entity}.cs, ErrorMessages.cs, Result.cs
  Application:     Features/{Feature}/I{Feature}Service.cs
                   Features/{Feature}/Dtos/{Operation}Input.cs, {Entity}Output.cs
                   Features/{Feature}/Persistence/I{Feature}Repository.cs
  Infrastructure:  Features/{Feature}/Services/{Feature}Service.cs
                   Features/{Feature}/Configurations/{Entity}Configuration.cs
                   Features/{Feature}/Extensions/ServiceCollectionExtensions.cs
                   Persistence/MyProjectDbContext.cs
  WebApi:          Features/{Feature}/{Feature}Controller.cs
                   Features/{Feature}/{Feature}Mapper.cs
                   Features/{Feature}/Dtos/{Operation}/{Operation}Request.cs
                   Features/{Feature}/Dtos/{Operation}/{Operation}RequestValidator.cs
                   Program.cs
```

### Frontend Naming Patterns

```
src/frontend/src/
  lib/api/          client.ts, error-handling.ts, v1.d.ts (generated)
  lib/components/   {feature}/{Name}.svelte + index.ts (barrel)
  lib/components/ui/{component}/  (shadcn — generated)
  lib/state/        {feature}.svelte.ts
  lib/types/        index.ts (type aliases)
  messages/         en.json, cs.json
  routes/(app)/     {feature}/+page.svelte, +page.server.ts
  routes/(public)/  login/+page.svelte
  styles/           themes.css, tailwind.css, animations.css
```

### Singleton Files (no pattern — memorize these)

| File | Why it matters |
|---|---|
| `src/backend/MyProject.WebApi/Program.cs` | DI wiring, middleware pipeline |
| `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs` | DbSets, migrations |
| `src/backend/MyProject.Domain/ErrorMessages.cs` | All static error strings |
| `src/backend/MyProject.Application/Identity/Constants/AppRoles.cs` | Role definitions |
| `src/backend/Directory.Packages.props` | NuGet versions (never in .csproj) |
| `src/frontend/src/lib/components/layout/SidebarNav.svelte` | Navigation entries |
| `src/frontend/src/lib/api/v1.d.ts` | Generated types (never hand-edit) |
| `.env.example` | Environment variable defaults |
| `docker-compose.local.yml` | Local dev service wiring |
