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

## File Location Index

### Backend

| Looking for... | Path |
|---|---|
| Solution file | `src/backend/MyProject.slnx` |
| Shared build props | `src/backend/Directory.Build.props` |
| NuGet versions | `src/backend/Directory.Packages.props` |
| .NET SDK version | `global.json` |
| dotnet-ef tool | `.config/dotnet-tools.json` |
| **Domain entity** | `src/backend/MyProject.Domain/Entities/{Entity}.cs` |
| Result pattern | `src/backend/MyProject.Domain/Result.cs` |
| Error messages | `src/backend/MyProject.Domain/ErrorMessages.cs` |
| **Service interface** | `src/backend/MyProject.Application/Features/{Feature}/I{Feature}Service.cs` |
| Application DTOs | `src/backend/MyProject.Application/Features/{Feature}/Dtos/` |
| Repository interface | `src/backend/MyProject.Application/Features/{Feature}/Persistence/I{Feature}Repository.cs` |
| Generic repository | `src/backend/MyProject.Application/Persistence/IBaseEntityRepository.cs` |
| Role constants | `src/backend/MyProject.Application/Identity/Constants/AppRoles.cs` |
| Cache keys | `src/backend/MyProject.Application/Caching/Constants/CacheKeys.cs` |
| Cookie names | `src/backend/MyProject.Application/Cookies/Constants/CookieNames.cs` |
| **Service implementation** | `src/backend/MyProject.Infrastructure/Features/{Feature}/Services/{Feature}Service.cs` |
| EF configuration | `src/backend/MyProject.Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs` |
| Base EF configuration | `src/backend/MyProject.Infrastructure/Persistence/Configurations/BaseEntityConfiguration.cs` |
| **DbContext** | `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs` |
| Generic repository impl | `src/backend/MyProject.Infrastructure/Persistence/BaseEntityRepository.cs` |
| Auditing interceptor | `src/backend/MyProject.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs` |
| Cache invalidation | `src/backend/MyProject.Infrastructure/Persistence/Interceptors/UserCacheInvalidationInterceptor.cs` |
| Pagination extension | `src/backend/MyProject.Infrastructure/Persistence/Extensions/PaginationExtensions.cs` |
| Options class (infra) | `src/backend/MyProject.Infrastructure/{Area}/Options/{Name}Options.cs` |
| DI extensions (infra) | `src/backend/MyProject.Infrastructure/{Area}/Extensions/ServiceCollectionExtensions.cs` |
| Seed users | `src/backend/MyProject.Infrastructure/Features/Authentication/Constants/SeedUsers.cs` |
| Identity models | `src/backend/MyProject.Infrastructure/Features/Authentication/Models/ApplicationUser.cs` |
| **Program.cs** | `src/backend/MyProject.WebApi/Program.cs` |
| **Controller** | `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Controller.cs` |
| Request/Response DTOs | `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/{Operation}/` |
| Mapper | `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Mapper.cs` |
| Validators | `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/{Operation}/{Operation}RequestValidator.cs` |
| Base controller | `src/backend/MyProject.WebApi/Shared/ApiController.cs` |
| ErrorResponse | `src/backend/MyProject.WebApi/Shared/ErrorResponse.cs` |
| Pagination shared | `src/backend/MyProject.WebApi/Shared/PaginatedRequest.cs`, `PaginatedResponse.cs` |
| Validation constants | `src/backend/MyProject.WebApi/Shared/ValidationConstants.cs` |
| Exception middleware | `src/backend/MyProject.WebApi/Middlewares/ExceptionHandlingMiddleware.cs` |
| CORS setup | `src/backend/MyProject.WebApi/Extensions/CorsExtensions.cs` |
| Rate limiting | `src/backend/MyProject.WebApi/Extensions/RateLimiterExtensions.cs` |
| Security headers | `src/backend/MyProject.WebApi/Extensions/SecurityHeaderExtensions.cs` |
| OpenAPI setup | `src/backend/MyProject.WebApi/Features/OpenApi/Extensions/WebApplicationBuilderExtensions.cs` |
| OpenAPI transformers | `src/backend/MyProject.WebApi/Features/OpenApi/Transformers/` |
| Options (WebApi) | `src/backend/MyProject.WebApi/Options/CorsOptions.cs`, `RateLimitingOptions.cs` |
| App settings | `src/backend/MyProject.WebApi/appsettings.json`, `appsettings.Development.json` |
| Dockerfile (backend) | `src/backend/MyProject.WebApi/Dockerfile` |
| EF migrations | `src/backend/MyProject.Infrastructure/Features/Postgres/Migrations/` |

### Frontend

| Looking for... | Path |
|---|---|
| **API client** | `src/frontend/src/lib/api/client.ts` |
| Error handling utils | `src/frontend/src/lib/api/error-handling.ts` |
| Generated API types | `src/frontend/src/lib/api/v1.d.ts` |
| Type aliases | `src/frontend/src/lib/types/index.ts` |
| **Auth helpers** | `src/frontend/src/lib/auth/auth.ts` |
| Server config | `src/frontend/src/lib/config/server.ts` |
| Client config / i18n meta | `src/frontend/src/lib/config/i18n.ts` |
| Config barrel | `src/frontend/src/lib/config/index.ts` |
| **Components (feature)** | `src/frontend/src/lib/components/{feature}/` |
| Components (shadcn UI) | `src/frontend/src/lib/components/ui/{component}/` |
| Sidebar navigation | `src/frontend/src/lib/components/layout/SidebarNav.svelte` |
| Header | `src/frontend/src/lib/components/layout/Header.svelte` |
| Theme toggle | `src/frontend/src/lib/components/layout/ThemeToggle.svelte` |
| Language selector | `src/frontend/src/lib/components/layout/LanguageSelector.svelte` |
| **Reactive state** | `src/frontend/src/lib/state/{feature}.svelte.ts` |
| Utils / cn() | `src/frontend/src/lib/utils/ui.ts` |
| Role utils | `src/frontend/src/lib/utils/roles.ts` |
| Platform detection | `src/frontend/src/lib/utils/platform.ts` |
| **Root layout** | `src/frontend/src/routes/+layout.svelte` |
| Root server load | `src/frontend/src/routes/+layout.server.ts` |
| Universal load (i18n) | `src/frontend/src/routes/+layout.ts` |
| Error page | `src/frontend/src/routes/+error.svelte` |
| Server hooks | `src/frontend/src/hooks.server.ts` |
| HTML template | `src/frontend/src/app.html` |
| **App layout** | `src/frontend/src/routes/(app)/+layout.svelte` |
| App auth guard | `src/frontend/src/routes/(app)/+layout.server.ts` |
| **Route page** | `src/frontend/src/routes/(app)/{feature}/+page.svelte` |
| Route server load | `src/frontend/src/routes/(app)/{feature}/+page.server.ts` |
| Login page | `src/frontend/src/routes/(public)/login/+page.svelte` |
| API proxy | `src/frontend/src/routes/api/[...path]/+server.ts` |
| Health proxy | `src/frontend/src/routes/api/health/+server.ts` |
| **i18n English** | `src/frontend/src/messages/en.json` |
| **i18n Czech** | `src/frontend/src/messages/cs.json` |
| Styles entry point | `src/frontend/src/styles/index.css` |
| Theme variables | `src/frontend/src/styles/themes.css` |
| Tailwind tokens | `src/frontend/src/styles/tailwind.css` |
| Base styles | `src/frontend/src/styles/base.css` |
| Animations | `src/frontend/src/styles/animations.css` |
| Utilities | `src/frontend/src/styles/utilities.css` |
| SvelteKit config | `src/frontend/svelte.config.js` |
| Vite config | `src/frontend/vite.config.ts` |
| shadcn config | `src/frontend/components.json` |
| ESLint config | `src/frontend/eslint.config.js` |
| Prettier config | `src/frontend/.prettierrc` |
| Dockerfile (frontend) | `src/frontend/Dockerfile` |
| Dockerfile (local dev) | `src/frontend/Dockerfile.local` |

### Root / Cross-Cutting

| Looking for... | Path |
|---|---|
| Docker Compose (local) | `docker-compose.local.yml` |
| Environment template | `.env.example` |
| Deploy scripts | `deploy.sh`, `deploy.ps1`, `deploy.config.json` |
| Init scripts | `init.sh`, `init.ps1` |
| Claude instructions | `CLAUDE.md` |
| Agent guidelines | `AGENTS.md` |
| Skills cookbook | `SKILLS.md` |
| Session docs | `docs/sessions/` |
| Session template | `docs/sessions/README.md` |
| Claude Code settings | `.claude/settings.local.json` |
| Claude Code commands | `.claude/commands/` |
