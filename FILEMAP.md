# File Map - Change Impact Reference

Quick-reference for "when you change X, also update Y" and "where does X live?"

> **Rule:** Before modifying any existing file listed here, trace its impact row. If a change affects downstream files, update them in the same commit (or same PR at minimum).

---

## Top 5 Most Common Changes

| Change | Must also update |
|---|---|
| **Add/change domain entity property** | EF config → migration → Application DTOs → WebApi DTOs → mapper → `pnpm run api:generate` → frontend |
| **Add backend endpoint** | Controller + DTOs + validator + mapper → `pnpm run api:generate` → frontend types → frontend calls |
| **Change WebApi response DTO** | Mapper, `Api.Tests/Contracts/ResponseContracts.cs`, `pnpm run api:generate`, frontend components |
| **Add permission** | `AppPermissions.cs` → `[RequirePermission]` → seed via `DefaultPermissions` in `AppRoles.Definitions` → frontend `permissions.ts` → sidebar + page guards |
| **Add i18n key** | Add to the correct feature file in all locale directories under `messages/` |

---

## Change Impact Tables

### Backend Changes

| When you change... | Also update... |
|---|---|
| **Domain entity** (add/rename property) | EF configuration, migration, Application DTOs, WebApi DTOs, mapper, frontend types (`pnpm run api:generate`) |
| **Domain entity** (add enum property) | EF config (`.HasComment()`), `EnumSchemaTransformer` handles the rest automatically |
| **`ErrorMessages.cs`** (Shared - add/rename `Error` entry) | Service that uses it; `ErrorMessagesTests` (code must be `{nested_class}_{field_name}` snake_case); frontend `ErrorMessagesByCode` maps keyed on the code (renaming a code is a breaking API change) |
| **`Result.cs`** / **`Error.cs`** (Shared - change pattern) | Every service + every controller that matches on `Result`; `ProblemFactory` |
| **`ProblemFactory`** (WebApi - ProblemDetails shape, `code` extension) | `ProblemDetailsSchemaTransformer` (OpenAPI), `ExceptionHandlingMiddleware`, `OriginValidationMiddleware`, `RateLimiterExtensions`, `ProblemDetailsAuthorizationHandler`, `Program.cs` (`EnsureCode`), `ProblemDetailsAssert` + `ProblemDetailsCodeTests` (Api.Tests), frontend `v1.d.ts` regeneration |
| **Application interface** (change signature) | Infrastructure service implementation, controller calling the service |
| **Application DTO** (add/rename/remove field) | Infrastructure service, WebApi mapper, WebApi request/response DTO, frontend types |
| **Infrastructure Options class** | `appsettings.json`, `appsettings.Development.json` (excluded from production publish - see `StripDevConfig`), DI registration |
| **WebApi controller** (change route/method) | Frontend API calls, `v1.d.ts` regeneration |
| **WebApi request DTO** (add/rename/remove property) | Validator, mapper, frontend types, frontend form |
| **WebApi response DTO** (add/rename/remove property) | Mapper, frontend types, frontend component displaying data, `Api.Tests/Contracts/ResponseContracts.cs` |
| **`Program.cs`** (change middleware order) | Test full request pipeline - order matters for auth, CORS, rate limiting; update `CustomWebApplicationFactory` if new services need mocking |
| **`BaseEntity.cs`** | `BaseEntityConfiguration`, `AuditingInterceptor`, all entities |
| **`FileStorageOptions`** (change S3/MinIO config) | `appsettings.json`, `MyProject.AppHost/Program.cs` (`.WithEnvironment()`), `appsettings.Testing.json` |
| **`EmailOptions`** (change config shape) | `appsettings.json`, `appsettings.Development.json`, `appsettings.Testing.json`, `ServiceCollectionExtensions` (email DI), `EmailOptionsValidationTests`, `EmailServiceRegistrationTests` |
| **`IEmailService`** (change sending contract) | `NoOpEmailService`, `SmtpEmailService`, `BackgroundEmailService`, `EmailDeliveryJob`, `CustomWebApplicationFactory` |
| **`SmtpEmailService`** (change SMTP delivery) | `EmailDeliveryJob` (job executor), `SmtpEmailServiceTests`, `EmailDeliveryJobTests` |
| **`EmailDeliveryJob`** (change job signature/retry policy) | `BackgroundEmailService` (enqueue expression), `EmailDeliveryJobTests`, `BackgroundEmailServiceTests`, `docs/features.md` |
| **Email DI routing** (`AddEmailServices` - background vs direct vs no-op) | `EmailServiceRegistrationTests`, `docs/before-you-ship.md` |
| **`IEmailTemplateRenderer`** (change rendering contract) | `FluidEmailTemplateRenderer`, `TemplatedEmailSender`, `FluidEmailTemplateRendererTests` |
| **`ITemplatedEmailSender`** (change send-safe contract) | `TemplatedEmailSender`, all services calling `SendSafeAsync()` (`AuthenticationService`, `AdminService`), `TemplatedEmailSenderTests` |
| **`EmailTemplateModels.cs`** (add/rename model record) | Matching `.liquid` templates (variables must match snake_case model properties), `FluidEmailTemplateRenderer.CreateOptions()` (register new model type), services that construct the model, `FluidEmailTemplateRendererTests` |
| **`.liquid` email template** (change variable/layout) | Matching model record in `EmailTemplateModels.cs`, `_base.liquid` if layout change, `FluidEmailTemplateRendererTests` |
| **`_base.liquid`** (shared email layout) | All rendered HTML emails, `FluidEmailTemplateRendererTests` layout assertions |
| **`FluidEmailTemplateRenderer`** (change rendering/caching logic) | `FluidEmailTemplateRendererTests`, `TemplatedEmailSender` |
| **`TemplatedEmailSender`** (change render+send wrapping) | `TemplatedEmailSenderTests`, `AuthenticationService`, `AdminService` |
| **`IFileStorageService`** (change upload/download contract) | `S3FileStorageService`, `UserService` (avatar ops), any future consumer |
| **`IImageProcessingService`** (change avatar processing) | `ImageProcessingService`, `UserService.UploadAvatarAsync` |
| **`ApplicationUser.HasAvatar`** (change avatar flag) | `UserOutput`, `AdminUserOutput`, `UserResponse`, `AdminUserResponse`, `UserMapper`, `AdminMapper`, frontend `v1.d.ts` types, `ProfileHeader.svelte`, `UserNav.svelte` |
| **Avatar endpoints** (`PUT/DELETE/GET`) | `UploadAvatarRequest`, `UploadAvatarRequestValidator`, `UserMapper`, frontend `AvatarDialog.svelte` |
| **`AuditActions.cs`** (add action constant) | Service that logs it, frontend `$lib/utils/audit.ts` (label, color, icon), i18n keys in `messages/{locale}/audit.json` |
| **`AuditEvent` entity** (change fields) | `AuditEventConfiguration`, `AuditService`, Application DTOs (`AuditEventOutput`), WebApi DTOs, `AuditMapper`, frontend types |
| **`HybridCache`** (caching abstraction - change caching usage) | `NoOpHybridCache`, `UserCacheInvalidationInterceptor`, all services using `HybridCache` (`AdminService`, `AuthenticationService`, `UserService`, `RoleManagementService`), `CustomWebApplicationFactory` mock |
| **`CacheKeys.cs`** (Application - rename/remove key) | All services referencing the changed key, `UserCacheInvalidationInterceptor` |
| **`CachingOptions`** (Infrastructure - change config shape) | `appsettings.json`, `appsettings.Development.json` |
| **`ICookieService`** (Application - change cookie contract) | `CookieService`, `AuthenticationService`, `UserService` |
| **`CookieNames`** (Application - rename/remove cookie name) | `AuthController`, `AuthenticationService`, `UserService` |
| **`IUserService`** (Application/Identity - change user service contract) | `UserService`, `UsersController`, `CustomWebApplicationFactory` mock |
| **`IUserContext`** (Application/Identity - change context contract) | `UserContext`, `AuthenticationService`, `UserService`, `AuditingInterceptor`, `UsersController`, `AdminController` |
| **`EmailTemplateNames.cs`** (Application - add/rename template name) | Services constructing `SendSafeAsync()` calls, matching `.liquid` template files |
| **`AppRoles.cs`** (add role) | Add a `RoleDefinition` entry to `Definitions` (name, rank, flags, default permissions) - seeding upserts it automatically; `RoleManagementService` checks `AppRoles.All` for system role collisions |
| **`AppPermissions.cs`** (add permission) | Seed via `DefaultPermissions` in `AppRoles.Definitions`, add `[RequirePermission]` to endpoints, update frontend `$lib/utils/permissions.ts` |
| **`PiiMasker.cs`** (Shared - change masking rules) | `AdminMapper.WithMaskedPii` extensions, `AuthenticationService` and `AdminService` (masked audit metadata + logs), `PiiMaskerTests` (Unit.Tests), `AdminMapperPiiTests` |
| **`RequirePermission` attribute** (add to endpoint) | Remove any class-level `[Authorize(Roles)]`; ensure permission is defined in `AppPermissions.cs` |
| **`RoleManagementService`** (change role behavior) | Verify system role protection rules, check security stamp rotation, verify frontend role detail page |
| **`IRecurringJobDefinition`** (add new job) | Register in `ServiceCollectionExtensions.AddJobScheduling()`, job auto-discovered at startup |
| **Job scheduling config** (`ServiceCollectionExtensions.AddJobScheduling`) | `Program.cs` must call `AddJobScheduling()` and `UseJobScheduling()` |
| **`JobSchedulingOptions`** (change `Enabled` semantics) | Email DI (`AddEmailServices` reads `JobScheduling:Enabled` to pick `BackgroundEmailService`), `EmailServiceRegistrationTests` |
| **`RateLimitPolicies.cs`** (add/rename constant) | `RateLimiterExtensions.cs` policy registration, `RateLimitingOptions.cs` config class, `appsettings.json` section, `[EnableRateLimiting]` attribute on controllers |
| **`RateLimitingOptions.cs`** (add/rename option class) | `RateLimiterExtensions.cs`, `appsettings.json`, `appsettings.Development.json` |
| **`RateLimiterExtensions.cs`** (add policy) | Requires matching constant in `RateLimitPolicies.cs` and config in `RateLimitingOptions.cs` |
| **`HostingOptions.cs`** (change hosting config shape) | `HostingExtensions.cs`, `appsettings.json`, `appsettings.Development.json` |
| **`Dockerfile`** (backend - change build/publish steps) | `.dockerignore`, verify published files don't include dev/test config |
| **`Dockerfile`** (frontend - change build steps) | `.dockerignore`, `.npmrc` (copied into image for install-affecting settings), `docker.yml` build args. New `PUBLIC_*` SvelteKit env vars need `ARG`+`ENV` in Dockerfile (before `pnpm run build`), `--build-arg` in `docker.yml`. For runtime env vars, prefer `$env/dynamic/private` with SSR layout data |
| **`MyProject.WebApi.csproj`** (add appsettings file) | If non-production: add `CopyToPublishDirectory="Never"` and matching `rm -f` in `Dockerfile` |
| **`HealthCheckExtensions.cs`** (change endpoints/checks) | Frontend health proxy `+server.ts`, Dockerfile healthcheck command |
| **New infrastructure dependency** (DB, cache, storage, etc.) | `MyProject.AppHost/Program.cs` (add resource + `.WithReference()`/`.WithEnvironment()`) |
| **Connection string config** (change format/name) | Verify `MyProject.AppHost/Program.cs` environment variable mapping still works |
| **`MyProject.ServiceDefaults/Extensions.cs`** | All projects referencing ServiceDefaults, `Program.cs` `AddServiceDefaults()` call |
| **`MyProject.AppHost/Program.cs`** | Verify resource names match `ConnectionStrings:*` and `WithEnvironment` keys match `appsettings.json` option paths |
| **`ProblemDetailsAuthorizationHandler`** | `ProblemFactory.CreateProblemDetails`, `ErrorMessages.Auth` entries, `Program.cs` registration |
| **`CaptchaOptions`** (Infrastructure - Captcha config) | `appsettings.json`, `appsettings.Development.json`, `appsettings.Testing.json`, `TurnstileCaptchaService`, `ServiceCollectionExtensions` |
| **`TurnstileCaptchaService`** (Infrastructure - Captcha service) | `ICaptchaService` interface, `CaptchaOptions`, `AuthController` captcha gate |
| **`TurnstileWidget.svelte`** (Frontend - Captcha widget) | `RegisterForm.svelte`, `ForgotPasswordForm.svelte`, `app.d.ts` (`Window.turnstile`), `TURNSTILE_SITE_KEY` env var (runtime-configurable via `$env/dynamic/private` and SSR layout data) |

### Frontend Changes

| When you change... | Also update... |
|---|---|
| **`$lib/utils/permissions.ts`** (add permission) | Backend `AppPermissions.cs` must have matching constant; update components checking that permission |
| **`v1.d.ts`** (regenerated) | Type aliases in `$lib/types/index.ts`, any component using changed schemas |
| **`$lib/api/client.ts`** | Every component using `browserClient` or `createApiClient` |
| **`$lib/api/error-handling.ts`** | Components that call `getErrorMessage`, `getErrorCode`, `mapFieldErrors`, `isValidationProblemDetails`, `isRateLimited`, `getRetryAfterSeconds`; `mutation.ts` (wraps these utilities); OAuth callback page and `LoginForm` (`ErrorMessagesByCode` maps) |
| **`$lib/api/mutation.ts`** | All form components using `handleMutationError()` for rate-limit, validation, and generic error handling |
| **`$lib/state/cooldown.svelte.ts`** | Components that call `createCooldown` for rate limit button disable |
| **`$lib/config/server.ts`** | Server load functions that import `SERVER_CONFIG` |
| **`$lib/config/i18n.ts`** | `LanguageSelector`, root layout |
| **`hooks.server.ts`** | All server responses (security headers, locale) |
| **`svelte.config.js`** (CSP) | Test that scripts/styles/images still load; Turnstile needs `script-src` + `frame-src` for `challenges.cloudflare.com` |
| **`+layout.svelte`** (root) | OG/Twitter meta defaults, canonical URL, global title/description - all pages inherit these |
| **`robots.txt/+server.ts`** | Disallow rules for route groups - update when adding new protected route groups |
| **`sitemap.xml/+server.ts`** | Public routes list - update `publicRoutes` array when adding public (non-auth) pages |
| **`app.html`** | FOUC prevention, nonce attribute, theme init |
| **`UserManagementCard.svelte`** | Thin shell - delegates to `RoleManagement.svelte` and `AccountActions.svelte` |
| **i18n keys** (rename/remove) | Same key in all other locale directories, all `m.{key}()` usages |
| **i18n keys** (add) | Add to the correct feature file in all locale directories |
| **Layout components** (Sidebar, Header, ContentHeader) | All pages that use the app shell |
| **`AppSidebar.svelte`** | Navigation links for all pages; admin items are per-permission gated; search trigger opens command palette |
| **`ContentHeader.svelte`** | Breadcrumb route-to-label mapping; segment labels must match sidebar nav items; detail pages set `dynamicLabel` via `$lib/state/breadcrumb.svelte` |
| **`CommandPalette.svelte`** | Command palette navigation and actions; admin items are per-permission gated (must stay in sync with `AppSidebar.svelte` nav items) |
| **Admin `+page.server.ts`** (add permission guard) | Must check specific permission and redirect if missing |
| **Route `+layout.server.ts`** | All child routes that depend on parent data |
| **Styles (`themes.css`)** | `tailwind.css` mappings, components using the variables |
| **Styles (`tailwind.css`)** | Components using custom Tailwind tokens |
| **`components.json`** (shadcn config) | Future `pnpm dlx shadcn-svelte@latest add` commands |
| **`.npmrc`** (pnpm settings) | `Dockerfile`, `Dockerfile.local` (both COPY it), CI `--frozen-lockfile` behavior |
| **`src/test-setup.ts`** | All test files (provides global `$app/*` mocks; changes here affect every test) |
| **`src/test-utils.ts`** (shared test utilities) | All route-level test files that import `MOCK_USER`, `createMockLoadEvent`, `createMockCookies` |
| **`$lib/utils/jobs.ts`** (job formatting) | `JobTable.svelte`, `JobInfoCard.svelte`, `JobExecutionHistory.svelte` |

### Cross-Stack Changes

| When you change... | Also update... |
|---|---|
| **Backend endpoint route** | Frontend API calls + regenerate types |
| **Backend response shape** | Regenerate types → update frontend components |
| **Backend auth/cookie behavior** | Frontend `$lib/auth/middleware.ts` (refresh logic), `$lib/auth/auth.ts` |
| **`appsettings.Development.json`** (add dev config override) | Keep standalone defaults (ConnectionStrings, FileStorage, JWT key) in sync with `MyProject.AppHost/appsettings.json` parameters |
| **`.env.example`** (frontend) | `src/frontend/.env.test` if new `PUBLIC_*` var added |
| **`.env.test`** (frontend) | `ci.yml` loads it via `cp .env.test .env`; keep in sync with `.env.example` vars |
| **`MyProject.AppHost/Program.cs`** (add env var or parameter) | Verify `appsettings.Development.json` standalone defaults match |
| **CORS config** (`CorsExtensions.cs`) | Frontend dev server origin, `ALLOWED_ORIGINS` env var |
| **Rate limiting config** | Frontend may need retry/backoff logic |
| **`appsettings.json`** structure | Options class, `MyProject.AppHost/Program.cs` |
| **CI workflows** (`.github/workflows/`) | Verify `dorny/paths-filter` patterns match project structure |

---

## Key Files Quick Reference

Files that are frequently referenced in impact tables above. For anything not listed here, use Glob/Grep - the codebase follows predictable naming patterns documented in the convention skills.

### Backend Naming Patterns

```
src/backend/MyProject.{Layer}/
  Shared:          Result.cs, Error.cs, ErrorType.cs, ErrorMessages.cs, PhoneNumberHelper.cs, PiiMasker.cs
  Domain:          Entities/{Entity}.cs
  Application:     Features/{Feature}/I{Feature}Service.cs
                   Features/{Feature}/Dtos/{Operation}Input.cs, {Entity}Output.cs
                   Features/{Feature}/Persistence/I{Feature}Repository.cs
                   Features/Email/EmailTemplateNames.cs
                   Identity/IUserService.cs, IUserContext.cs
                   Identity/Constants/AppRoles.cs, AppPermissions.cs
                   Caching/Constants/CacheKeys.cs
                   Cookies/ICookieService.cs, Constants/CookieNames.cs
                   Persistence/IBaseEntityRepository.cs
  Infrastructure:  Features/{Feature}/Services/{Feature}Service.cs
                   Features/{Feature}/Configurations/{Entity}Configuration.cs
                   Features/{Feature}/Extensions/ServiceCollectionExtensions.cs
                   Persistence/MyProjectDbContext.cs
  WebApi:          Features/{Feature}/{Feature}Controller.cs
                   Features/{Feature}/{Feature}Mapper.cs
                   Features/{Feature}/Dtos/{Operation}/{Operation}Request.cs
                   Features/{Feature}/Dtos/{Operation}/{Operation}RequestValidator.cs
                   Authorization/RequirePermissionAttribute.cs (+ handler, provider, requirement)
                   Authorization/ProblemDetailsAuthorizationHandler.cs
                   Routing/{Name}RouteConstraint.cs
                   Shared/RateLimitPolicies.cs, ProblemFactory.cs
                   Features/OpenApi/Transformers/{Name}SchemaTransformer.cs
                   Program.cs
```

### Frontend Naming Patterns

```
src/frontend/src/
  lib/api/          client.ts, error-handling.ts, mutation.ts, backend-monitor.ts, v1.d.ts (generated)
  lib/components/   {feature}/{Name}.svelte + index.ts (barrel)
  lib/components/ui/{component}/  (shadcn - generated)
  lib/state/        {feature}.svelte.ts
  lib/types/        index.ts (type aliases)
  lib/utils/        ui.ts (cn()), permissions.ts, audit.ts, platform.ts, roles.ts, jobs.ts
  messages/         {locale}/*.json (core, dashboard, auth, admin, jobs, audit, 2fa, oauth, avatars)
  routes/(app)/     {feature}/+page.svelte, +page.server.ts
  routes/(public)/  login/+page.svelte
  styles/           themes.css, tailwind.css, animations.css
```

### Job Scheduling Patterns

```
src/backend/MyProject.Infrastructure/Features/Jobs/
  IRecurringJobDefinition.cs                          Interface for recurring jobs
  RecurringJobs/{JobName}Job.cs                       Recurring job implementations
  Examples/ExampleFireAndForgetJob.cs                 Example one-time job (removable)
src/backend/MyProject.Infrastructure/Features/Email/Jobs/
  EmailDeliveryJob.cs                                 Real one-time job: retrying SMTP delivery
  Models/PausedJob.cs                                 Persisted pause state entity
  Configurations/PausedJobConfiguration.cs            EF config → hangfire.pausedjobs
  Services/JobManagementService.cs                    Admin API service (DB-backed pause)
  Options/JobSchedulingOptions.cs                     Configuration (Enabled, WorkerCount)
  Extensions/ServiceCollectionExtensions.cs           DI registration
  Extensions/ApplicationBuilderExtensions.cs          Middleware + job registration + pause restore
src/backend/MyProject.Application/Features/Jobs/
  IJobManagementService.cs                            Admin API interface
  Dtos/RecurringJobOutput.cs, ...                     Job DTOs
src/backend/MyProject.WebApi/Features/Admin/
  JobsController.cs                                   Admin job endpoints
  JobsMapper.cs                                       DTO mapping
  Dtos/Jobs/                                          Response DTOs
```

### Email Template Patterns

```
src/backend/MyProject.Application/Features/Email/
  IEmailTemplateRenderer.cs                         Rendering interface (Render<TModel>)
  ITemplatedEmailSender.cs                          Safe render+send interface (SendSafeAsync)
  Models/EmailTemplateModels.cs                     Model records (one per template)
  EmailTemplateNames.cs                             Template name constants (kebab-case)
  IEmailService.cs                                  Sending interface
  EmailMessage.cs                                   Message envelope DTO
src/backend/MyProject.Infrastructure/Features/Email/
  Services/FluidEmailTemplateRenderer.cs            Fluid-based renderer (singleton, cached)
  Services/TemplatedEmailSender.cs                  Render+send wrapper (swallows failures)
  Services/BackgroundEmailService.cs                Queues EmailDeliveryJob (Enabled + JobScheduling:Enabled)
  Services/SmtpEmailService.cs                      MailKit SMTP sender (direct when jobs disabled; job executor otherwise)
  Services/NoOpEmailService.cs                      Dev/test no-op sender (when disabled)
  Jobs/EmailDeliveryJob.cs                          Hangfire job: SMTP send with [AutomaticRetry] backoff
  Templates/_base.liquid                            Shared HTML email layout (header, card, footer)
  Templates/{name}.liquid                           HTML body fragment
  Templates/{name}.text.liquid                      Plain text variant (optional)
  Templates/{name}.subject.liquid                   Subject line
  Options/EmailOptions.cs                           FromName, FrontendBaseUrl config
  Extensions/ServiceCollectionExtensions.cs         DI registration (AddEmailServices)
```

### Test Naming Patterns

```
src/backend/tests/
  MyProject.Unit.Tests/
    {Layer}/{ClassUnderTest}Tests.cs             Unit tests (pure logic)
  MyProject.Component.Tests/
    Fixtures/TestDbContextFactory.cs             InMemory DbContext factory
    Fixtures/IdentityMockHelpers.cs              UserManager/RoleManager mock setup
    Services/{Service}Tests.cs                   Service tests (mocked deps)
  MyProject.Api.Tests/
    Fixtures/CustomWebApplicationFactory.cs      WebApplicationFactory config
    Fixtures/TestAuthHandler.cs                  Fake auth handler
    Fixtures/ProblemDetailsAssert.cs             Shared ProblemDetails (status/detail/code) assertions
    Shared/ProblemFactoryTests.cs                ProblemFactory + code fallback tests
    Middlewares/ProblemDetailsCodeTests.cs       End-to-end `code` presence across the pipeline
    Contracts/ResponseContracts.cs               Frozen response shapes for contract testing
    Controllers/{Controller}Tests.cs             HTTP integration tests
    Validators/{Validator}Tests.cs               FluentValidation tests
  MyProject.Architecture.Tests/
    DependencyTests.cs                           Layer dependency rules
    NamingConventionTests.cs                     Class naming enforcement
    AccessModifierTests.cs                       Visibility rules
```

### Singleton Files (no pattern - memorize these)

| File | Why it matters |
|---|---|
| `src/backend/MyProject.WebApi/Program.cs` | DI wiring, middleware pipeline |
| `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs` | DbSets, migrations |
| `src/backend/MyProject.Shared/ErrorMessages.cs` | All client-facing errors (`Error` = stable snake_case code + message) |
| `src/backend/MyProject.Application/Identity/Constants/AppRoles.cs` | Declarative role definitions (`RoleDefinition`: rank, flags, default permissions) |
| `src/backend/MyProject.Application/Identity/PermissionEvaluator.cs` | Single authorization decision point (wildcard-aware); consumed by `PermissionAuthorizationHandler` and `UserContext` |
| `src/backend/MyProject.Application/Identity/Constants/AppPermissions.cs` | Permission definitions (reflection-discovered) |
| `src/backend/MyProject.Application/Caching/Constants/CacheKeys.cs` | Cache key constants (used across services) |
| `src/backend/MyProject.Application/Features/Email/EmailTemplateNames.cs` | Email template name constants |
| `src/frontend/src/lib/utils/permissions.ts` | Frontend permission constants + helpers |
| `src/backend/MyProject.WebApi/Shared/RateLimitPolicies.cs` | Rate limit policy name constants |
| `src/backend/Directory.Packages.props` | NuGet versions (never in .csproj) |
| `src/frontend/src/lib/components/layout/AppSidebar.svelte` | Navigation entries + command palette trigger |
| `src/frontend/src/lib/components/layout/ContentHeader.svelte` | Desktop breadcrumb header + sidebar toggle (keep route labels in sync with AppSidebar) |
| `src/frontend/src/lib/components/layout/CommandPalette.svelte` | Command palette entries (keep in sync with AppSidebar) |
| `src/frontend/src/lib/api/v1.d.ts` | Generated types (never hand-edit) |
| `src/frontend/.env.test` | CI + test environment defaults (loaded by `ci.yml`) |
| `src/backend/MyProject.WebApi/appsettings.Testing.json` | Test environment config (disables Hangfire, caching, CORS) |
| `src/backend/tests/MyProject.Api.Tests/Fixtures/CustomWebApplicationFactory.cs` | Test host configuration for API tests |
| `src/backend/MyProject.ServiceDefaults/Extensions.cs` | Aspire shared: OTEL, service discovery, HTTP resilience defaults |
| `src/backend/MyProject.AppHost/Program.cs` | Aspire orchestrator: local dev (PostgreSQL, MinIO, MailPit, API, Frontend) |
