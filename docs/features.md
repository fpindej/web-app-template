# Features

> Back to [README](../README.md)

## Backend - .NET 10 / C# 13

| Feature | Implementation |
|---|---|
| **Clean Architecture** | Domain, Application, Infrastructure, WebApi - with [architecture tests](../src/backend/tests/MyProject.Architecture.Tests) enforcing dependency rules at build time |
| **Authentication** | JWT in HttpOnly cookies, refresh token rotation with reuse detection (stolen token revokes entire family), security stamp validation, remember-me persistent sessions |
| **Two-Factor Authentication** | TOTP-based 2FA with QR code setup, 6-digit verification, 10 single-use recovery codes, challenge token flow (login returns challenge, client submits TOTP code), admin can disable 2FA for locked-out users with automatic session revocation and notification email |
| **OAuth / External Login** | OAuth 2.0 / OIDC external authentication with 10 providers out of the box - Google, GitHub, Discord, Apple, Microsoft, Facebook, LinkedIn, GitLab, Slack, and Twitch. Manual `HttpClient`-based code exchange (no ASP.NET middleware). Auto-links accounts by verified email. 2FA is bypassed for OAuth logins (provider already verified identity). Provider credentials stored with AES-256-GCM encryption |
| **OAuth Admin Management** | Admins configure OAuth providers entirely from the UI - enable/disable providers, set client ID and secret, configure scopes and endpoints, test connection with a single click. No redeploy required. Credentials encrypted at rest with AES-256-GCM |
| **Authorization** | Permission-based with custom roles - atomic permissions (`users.view`, `roles.manage`, ...) assigned per role, enforced via `[RequirePermission]` attribute. Roles flagged `GrantsAllPermissions` (Superuser) have implicit full access via a wildcard permission claim |
| **Role Hierarchy** | Data-driven ranks (Superuser > Admin > User, custom roles rank 0) - privilege escalation prevention, self-protection rules, system role guards, lockout protection for the last all-permissions holder. Custom roles with arbitrary permission sets |
| **Rate Limiting** | Global + per-endpoint policies (registration, auth, sensitive operations, admin mutations), configurable fixed-window with IP and user partitioning |
| **Validation** | FluentValidation + Data Annotations, flowing constraints into OpenAPI spec and generated TypeScript types |
| **Caching** | HybridCache (in-process L1) with auto-invalidation via EF Core interceptor, stampede protection, key management |
| **Database** | PostgreSQL + EF Core with soft delete, full audit trail (created/updated/deleted by + at), global query filters |
| **Audit Trail** | Append-only audit events table with JSONB metadata, action constants covering auth, account, admin, role, and OAuth operations. Admin per-user view and user self-activity log |
| **API Documentation** | OpenAPI spec + Scalar UI, with custom transformers for enums, nullable types, numeric schemas, and camelCase query params |
| **Error Handling** | Result pattern for business logic, `ProblemDetails` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) on every error response with a stable machine-readable `code` extension alongside the human-readable `detail` |
| **Logging** | Serilog bridged to OpenTelemetry with structured request logging, correlation, and Aspire Dashboard |
| **Account Management** | Registration with CAPTCHA, login/logout, remember me, email verification, password reset, profile updates with avatar upload, account deletion, connected OAuth accounts management, set password for OAuth-only users |
| **Admin Panel API** | User CRUD with search/pagination, custom role management with permission editor, role assignment with hierarchy enforcement, background job management, OAuth provider configuration, 2FA management |
| **Background Jobs** | Hangfire with PostgreSQL persistence - recurring jobs via `IRecurringJobDefinition`, fire-and-forget, admin dashboard with trigger/pause/resume/restore, persistent pause state |
| **Email** | MailKit SMTP with configurable enable/disable toggle, Fluid (Liquid) template engine with shared HTML base layout. Resilient delivery: when job scheduling is enabled, sends are queued as Hangfire jobs with automatic retries and exponential backoff (5 attempts, 30s to 1h), failed sends stay in Hangfire storage (visible in the development dashboard) for manual retry. Templates for verification, password reset, admin-initiated reset, invitation, and 2FA disable notification. MailPit integration for local email testing via Aspire |
| **File Storage** | S3-compatible blob storage via `IFileStorageService` - MinIO locally, any S3 provider in production (AWS S3, Cloudflare R2, DigitalOcean Spaces, Backblaze B2). Avatar upload with SkiaSharp image processing (resize to 512x512, WebP compression) |
| **PII Compliance** | `users.view_pii` permission gates personal data visibility. Emails masked to `j***@g***.com`, phone numbers to `***`. Self-view exemption. No PII in logs, URLs, or error responses |
| **Health Checks** | `/health` (all), `/health/ready` (DB + S3), `/health/live` (liveness) - Docker healthcheck integration |
| **Search** | User lookup with search and pagination, PostgreSQL trigram similarity function pre-registered for custom use |
| **Testing** | 4 test projects - unit, component (mocked services), API integration (WebApplicationFactory), architecture enforcement. Comprehensive coverage of auth flows, 2FA, OAuth, permissions, rate limiting, and response contracts |

## Frontend - SvelteKit / Svelte 5

| Feature | Implementation |
|---|---|
| **Svelte 5 Runes** | Modern reactivity with `$state`, `$derived`, `$effect` - no legacy stores or `export let` |
| **Type-Safe API Client** | Generated from OpenAPI spec via `openapi-typescript` - backend changes break the build, not the user |
| **shadcn-svelte UI** | Full component library built on bits-ui headless primitives - Button, Card, Dialog, Table, Sidebar, Command, Breadcrumb, InputOTP, Tooltip, and more. CSS variable theming with Tailwind CSS 4 |
| **Command Palette** | Cmd+K (Mac) / Ctrl+K (Windows/Linux) command palette for quick navigation. Permission-gated - users only see pages they can access. Keyboard shortcut help dialog (Shift+?) |
| **OAuth Login** | "Sign in with" buttons for all configured providers, automatic redirect flow, connected accounts management in user settings, provider-specific icons |
| **Two-Factor Authentication UI** | QR code TOTP setup wizard, shadcn InputOTP with auto-submit, recovery codes display with copy-to-clipboard, disable confirmation dialog |
| **Automatic Token Refresh** | 401 interceptor with refresh and retry, transparent to components, thundering-herd protection |
| **BFF Architecture** | Server-side API proxy handles cookies, CSRF validation, header filtering, and `X-Forwarded-For` propagation |
| **i18n** | Paraglide JS - type-safe keys, compile-time validation, SSR-compatible, auto-detection via Accept-Language. Per-feature message files, add any locale |
| **Security Headers** | CSP with nonce mode, HSTS, X-Frame-Options, Referrer-Policy, Permissions-Policy on every response |
| **Permission Guards** | Layout-level + page-level route guards, per-permission nav item filtering, component-level conditional rendering |
| **Dark Mode** | Light/dark/system theme with localStorage persistence, FOUC prevention, CSS variable theming |
| **Responsive Design** | Mobile-first with sidebar drawer, desktop content header with breadcrumbs, 44px touch targets on all interactive elements, logical CSS properties for RTL readiness, safe-area insets for notched devices |
| **Loading States** | Skeleton components and loading spinners for async content throughout the admin panel |
| **Error Handling** | Unified mutation error handler - rate limiting with visible cooldown timers, field-level validation with shake animations, generic errors with toast notifications |
| **Admin UI** | User table with search/pagination and PII masking, role card grid with permission editor, job dashboard with execution history, OAuth provider management with toggle switches and test connection, 2FA management |
| **Avatar Upload** | Drag-and-drop with client-side crop tool, preview, SkiaSharp server-side compression to WebP, S3 storage |
| **Auth Pages** | Login with API health indicator, registration with form draft persistence, forgot/reset password flows, email verification - all with CAPTCHA integration and smooth transitions |

## Infrastructure & DevOps

| Feature | Implementation |
|---|---|
| **Aspire Local Dev** | One `dotnet run` for the full stack - API, frontend (hot-reload), PostgreSQL, MinIO (S3 storage), MailPit (email testing), OpenTelemetry Dashboard with traces, logs, and metrics |
| **Production Docker** | Multi-stage Dockerfiles for API (.NET) and frontend (Node), dev config stripping, non-root users, health probe binary. Deploy to any platform - Docker Compose, Coolify, Railway, Kubernetes |
| **Init Script** | Interactive project bootstrapping - renames solution, configures ports, creates migration, launches Aspire. Works on macOS, Linux, and Windows |
| **CI Pipeline** | GitHub Actions with smart path filtering - backend-only PRs skip frontend checks and vice versa. Coverage reports with ReportGenerator |
| **Docker Validation** | CI validates image builds on Dockerfile/dependency changes, with layer caching |
| **Dependabot** | Weekly NuGet, npm, and GitHub Actions updates with grouped minor+patch PRs |
| **Environment Config** | `appsettings.json` defines all options, `appsettings.Development.json` provides working dev defaults, env vars override for production |
| **Production Hardening** | Dev config stripping from production images, reverse proxy trust configuration, CORS startup safeguard (crashes on `AllowAllOrigins=true` in production) |
| **Claude Code Skills** | 20+ native skills for development workflows - feature scaffolding, endpoint creation, PR management, design review, type generation. 13 specialized agents (engineers, reviewers, UX designer, product owner, tech writer), lifecycle hooks, and context files (`CLAUDE.md`, `FILEMAP.md`) for deep codebase understanding |

## What Your Users Get

NETrock is not just a developer tool - the included frontend delivers features that end users of your product interact with directly:

- **Sign in with their existing accounts** - Google, GitHub, Discord, Apple, Microsoft, Facebook, LinkedIn, GitLab, Slack, Twitch. Admins enable providers from the UI without any redeploy
- **Two-factor authentication** - TOTP setup with QR code, recovery codes for account recovery. Admins can disable 2FA for locked-out users
- **Profile management** - Avatar upload with image cropping, profile editing, connected OAuth accounts management, password management
- **Dark mode and language switching** - Light/dark/system theme, language auto-detection with manual override
- **Keyboard-driven navigation** - Cmd+K command palette, global keyboard shortcuts, help dialog
- **Mobile-ready** - Responsive design with touch-friendly targets, safe-area support for notched devices, sidebar drawer on mobile
- **Security they can trust** - CAPTCHA on registration, visible rate limiting feedback, email verification, password reset flows
