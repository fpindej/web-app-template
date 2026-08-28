# Before You Ship

> Back to [README](../README.md)

NETrock works out of the box for local development, but there are things you need to configure before going to production. This checklist covers what the template **can't decide for you**.

> **Environment variables** are the primary configuration mechanism. Set them on your API and frontend containers however your platform supports it (env files, UI, secrets manager, etc.). The checklist below lists what needs configuring.

## Must Do

- [ ] **Email service** - configure a real SMTP provider for production. In development, email is enabled by default and routed to MailPit. If `Email__Enabled` is `false`, a `NoOpEmailService` logs emails via Serilog instead of sending them. When `JobScheduling__Enabled` is `true` (default), emails are queued as Hangfire jobs and retried automatically on SMTP failure; failed deliveries end up as failed jobs in Hangfire storage. Note that the rendered email (recipient, subject, body including verification/reset links) is persisted as the job payload in the `hangfire` schema until the job expires (24h after success by default; failed jobs stay until deleted), so protect database access accordingly. Configure via `Email__Smtp__*` env vars
- [ ] **CORS origins** - set `Cors__AllowedOrigins__0` to your production domain (add `__1`, `__2` for additional origins). The app **will refuse to start** if `AllowAllOrigins` is `true` outside of Development - this is intentional
- [ ] **JWT secret** - the init script generates a random 64-char key in `appsettings.json`. For production, set `Authentication__Jwt__Key` as an environment variable (minimum 64 chars, cryptographically random)
- [ ] **Database** - point `ConnectionStrings__Database` to your production PostgreSQL instance
- [ ] **CAPTCHA keys** - replace the Cloudflare Turnstile development keys with production keys (`Captcha__SecretKey` backend, `TURNSTILE_SITE_KEY` frontend - runtime-configurable via the `(public)` layout server load)
- [ ] **Frontend URL in emails** - set `Email__FrontendBaseUrl` to your production domain so email verification and password reset links work
- [ ] **Bootstrap admin** - set `Seed__Users__0__Email`, `Seed__Users__0__Password`, and `Seed__Users__0__Role=Superuser` environment variables to create an initial Superuser on first deploy. Idempotent - safe to leave set, but remove after creating admin accounts through the UI
- [ ] **File storage** - configure `FileStorage__*` env vars for your S3-compatible provider. Local dev uses MinIO (included in Aspire). For production, point to your preferred provider - AWS S3, Cloudflare R2, DigitalOcean Spaces, Backblaze B2, or any S3-compatible service. Set `FileStorage__Endpoint`, `FileStorage__AccessKey`, `FileStorage__SecretKey`, `FileStorage__BucketName`, `FileStorage__Region` (if applicable), and `FileStorage__UseSSL=true`. Use `/manage-file-storage` skill for provider-specific configs or to remove file storage entirely
- [ ] **OAuth encryption key** - set `Authentication__ExternalProviders__EncryptionKey` to a cryptographically random base64 string (32+ bytes). This key encrypts OAuth provider client secrets at rest with AES-256-GCM. Generate with `openssl rand -base64 32`. Without this, OAuth provider client secrets stay encrypted under the key the init script committed to git
- [ ] **OAuth providers** - configure OAuth providers from the admin UI after first deploy. Each provider needs a client ID and secret from the provider's developer console (Google Cloud Console, GitHub Developer Settings, etc.). Admins can enable/disable providers, test connections, and update credentials without redeploying

## Should Do

- [ ] **TLS termination** - the containers expose API (8080) and frontend (3000) as plain HTTP. Put a reverse proxy (nginx, Caddy, Traefik) in front to terminate TLS, or use your platform's built-in TLS (Coolify, Railway, etc.). Set `ORIGIN=https://your-domain.com` in the frontend env so SvelteKit generates correct URLs
- [ ] **Reverse proxy** - if behind nginx/load balancer, configure `Hosting__ReverseProxy__TrustedNetworks` and `TrustedProxies` so rate limiting uses real client IPs
- [ ] **Logging** - logs flow via OpenTelemetry. Set `OTEL_EXPORTER_OTLP_ENDPOINT` for your production collector (Grafana, Datadog, etc.). Locally, logs are visible in the Aspire Dashboard. Adjust log levels (`Serilog__MinimumLevel__Default=Information`)
- [ ] **Rate limits** - review the production defaults in `appsettings.json` and adjust for your expected traffic
- [ ] **Backups** - set up automated PostgreSQL backups. NETrock uses soft delete, but that doesn't replace real backups
- [ ] **Monitoring** - the health check endpoints (`/health`, `/health/ready`, `/health/live`) are ready for your uptime monitoring
- [ ] **Resource limits** - configure CPU/memory limits in your deployment platform. Recommended starting points: API 2 CPU / 1G, frontend 1 CPU / 512M, PostgreSQL 1 CPU / 1G. PostgreSQL alone typically wants 25% of available memory for `shared_buffers`

## Frontend

- [ ] **SEO meta tags** - the root layout includes default Open Graph and Twitter Card meta tags using `app_name` and `meta_description` from i18n. Update these in `src/frontend/src/messages/{locale}/core.json` with your actual product name and description. Add per-page `og:title` and `og:description` overrides in `<svelte:head>` for important public pages
- [ ] **Open Graph image** - add an `og:image` meta tag pointing to a 1200x630px preview image for social sharing. Place the image in `static/` and reference it with an absolute URL in the root layout
- [ ] **Sitemap** - the sitemap at `/sitemap.xml` only includes the root URL by default. Add your public routes to `src/frontend/src/routes/sitemap.xml/+server.ts` as you build them
- [ ] **Webmanifest** - update `name` and `short_name` in `src/frontend/static/site.webmanifest` to match your product name

## Good to Know

- **Hangfire dashboard** is automatically disabled in production. Use the admin API endpoints (`/api/admin/jobs/*`) instead
- **HTTPS** is forced in production via `Hosting__ForceHttps=true` (default). Development runs on HTTP
- **Dev config is stripped** from production Docker images - `appsettings.Development.json` and `appsettings.Testing.json` are removed at build time
- **CORS startup guard** will crash the app on purpose if you deploy with `AllowAllOrigins=true` - this is a security feature, not a bug
