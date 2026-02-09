# Getting Started

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/) (for local frontend development)
- [Git](https://git-scm.com/)

## First-Time Setup

### 1. Clone and Initialize

```bash
git clone <your-repo-url>
cd web-app-template
```

Run the init script to rename the project and configure ports:

```bash
# macOS / Linux
chmod +x init.sh && ./init.sh

# Windows
.\init.ps1
```

The script:
1. Asks for a **Project Name** (e.g., `MyAwesomeApi`) — renames all files, namespaces, and directories.
2. Asks for a **Base Port** (default `13000`) — Frontend: base port, API: base+2, Database: base+4.
3. Generates a random JWT secret in `.env`.
4. Restores local .NET tools (`dotnet-ef`).

### 2. Start Services

```bash
docker compose -f docker-compose.local.yml up -d
```

That's it. All services start with working defaults.

| Service | URL |
|---|---|
| Frontend | `http://localhost:{BASE_PORT}` |
| API | `http://localhost:{BASE_PORT+2}` |
| API docs (Scalar) | `http://localhost:{BASE_PORT+2}/scalar/v1` |
| Seq (logs) | `http://localhost:{BASE_PORT+6}` |

### 3. Without the Init Script

If the init script has already been run (or you're on an existing clone):

```bash
cp .env.example .env
docker compose -f docker-compose.local.yml up -d
```

`.env.example` has working defaults for everything — no edits required.

---

## Developer Workflows

### Run Everything in Docker (Default)

```bash
docker compose -f docker-compose.local.yml up -d
```

No config changes needed. The frontend, API, database, Redis, and Seq all start with defaults.

### Frontend Dev — Tweak Backend Config

Edit `.env` (not `.env.example`), change what you need, restart Docker:

```bash
# Example: longer JWT tokens, relaxed rate limit
Authentication__Jwt__ExpiresInMinutes=300
RateLimiting__Global__PermitLimit=1000
```

```bash
docker compose -f docker-compose.local.yml up -d
```

ASP.NET automatically picks up `Section__Key` environment variables. No backend source files need to be touched.

### Backend Dev — Debug in Rider/VS with Frontend

This workflow lets you run the API from your IDE (with breakpoints) while the frontend runs in Docker:

1. Stop the API container: `docker compose -f docker-compose.local.yml stop api`
2. In `.env`, uncomment: `API_URL=http://host.docker.internal:5142`
3. Restart the frontend container: `docker compose -f docker-compose.local.yml restart frontend`
4. Launch the API from Rider with the "Development - http" profile (port 5142)
5. Use the frontend at `localhost:{FRONTEND_PORT}` — it proxies API calls to Rider

The backend loads `appsettings.Development.json` which has `localhost` connection strings for db/redis/seq (pointing at Docker-exposed ports). Breakpoints work normally.

### Testing on a Phone or Other Device

The app uses `Secure` + `SameSite=None` cookies, so testing over plain HTTP won't work — browsers silently discard `Secure` cookies on non-HTTPS origins. Use an HTTPS tunnel:

1. Install [ngrok](https://ngrok.com/) (or Tailscale Funnel, Cloudflare Tunnel, etc.)
2. Start the tunnel:
   ```bash
   ngrok http {FRONTEND_PORT}
   ```
3. Copy the HTTPS URL and add it to `.env`:
   ```bash
   ALLOWED_ORIGINS=https://abc123.ngrok-free.app
   ```
4. Recreate the frontend container:
   ```bash
   docker compose -f docker-compose.local.yml up -d frontend --force-recreate
   ```
5. Open the ngrok URL on your phone. Login works end-to-end.

**Why this is needed:**
- `ALLOWED_ORIGINS` tells the SvelteKit API proxy to accept CSRF requests from the tunnel's origin (otherwise it rejects POST with 403).
- Vite's dev server is configured with `allowedHosts: true` so it accepts requests from any hostname. This is safe — only the dev server is exposed, never production.

**Cleanup:** Remove the `ALLOWED_ORIGINS` line from `.env` and stop the tunnel when done.

---

## Environment Configuration

### Configuration Files

| File | Purpose | Who Edits It |
|---|---|---|
| `.env.example` | Working dev defaults — copy to `.env` to get started | Rarely |
| `.env` | Local overrides (git-ignored) — copied from `.env.example` | Everyone |
| `appsettings.json` | Base/production defaults | Backend devs |
| `appsettings.Development.json` | Dev defaults (generous JWT expiry, debug logging, localhost URLs) | Backend devs |
| `docker-compose.local.yml` | Docker service wiring (host-specific values only) | Rarely |

### How the API Container Reads Configuration

The API container loads `.env` in two ways:

1. **Variable interpolation** — `docker-compose.local.yml` references `${VAR}` / `${VAR:-default}` for values that need renaming or host-specific defaults (connection strings, secrets, ports).
2. **`env_file: .env`** — every variable in `.env` is also injected into the API container directly. ASP.NET picks up any `Section__Key` variable (e.g., `Authentication__Jwt__ExpiresInMinutes`) automatically.

### Precedence (Highest to Lowest)

| Priority | Source | Example |
|---|---|---|
| 1 | `docker compose run --env` | CLI override (rare) |
| 2 | Compose `environment` block | `Authentication__Jwt__Key: ${JWT_SECRET_KEY}` — interpolated from `.env` |
| 3 | Compose `env_file: .env` | `Authentication__Jwt__ExpiresInMinutes=100` — passes through directly |
| 4 | `appsettings.{Environment}.json` | `ExpiresInMinutes: 100` in `appsettings.Development.json` |
| 5 | `appsettings.json` | Base defaults (e.g., `ExpiresInMinutes: 10`) |

**In practice:** variables in the compose `environment` block (connection strings, secrets, Seq URL) always win. Everything else set in `.env` passes through to the container and overrides appsettings values. When running from Rider/VS (no Docker), only appsettings files apply — `.env` is not read.

---

## Database Migrations

When running the API in `Development` configuration, pending migrations are applied automatically on startup.

To add new migrations manually:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Features/Postgres/Migrations
```

To apply migrations manually:

```bash
dotnet ef database update \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi
```

---

## Pre-Commit Quality Checks

Before every commit, run:

- **Backend**: `dotnet build src/backend/MyProject.slnx`
- **Frontend**: `cd src/frontend && npm run format && npm run lint && npm run check`

Never commit code that doesn't compile, has lint errors, or fails type checks.

---

## Deployment

Build and push images via `./deploy.sh` (or `deploy.ps1`), configured by `deploy.config.json`. See the deploy scripts for details.
