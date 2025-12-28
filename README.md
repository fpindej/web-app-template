# Web APP Template

This repository serves as a robust starting point for building modern full-stack applications with .NET and SvelteKit. It comes pre-configured with essential components to jumpstart your development, following Clean Architecture principles.

## Features

### Backend (.NET 10)
*   **Clean Architecture:** Organized into Domain, Application, Infrastructure, and WebApi layers.
*   **Database:** Pre-configured PostgreSQL connection with Entity Framework Core.
*   **Identity:** Built-in authentication system (JWT/Cookie-based) with HttpOnly cookies.
*   **Validation:** FluentValidation integration.
*   **Logging:** Serilog configuration.
*   **Documentation:** Scalar (OpenAPI) integration.

### Frontend (SvelteKit)
*   **Modern Stack:** Svelte 5 (Runes), Tailwind CSS v4, and Vite.
*   **UI Components:** Shadcn-svelte (using bits-ui@next) for accessible, customizable components.
*   **BFF Pattern:** Backend-for-Frontend architecture using SvelteKit's server-side hooks and proxy routes to handle authentication securely.
*   **Type Safety:** End-to-end type safety with `openapi-fetch` generated from the backend OpenAPI spec.
*   **Localization:** Production-ready i18n system with type-safe keys, server-side detection, and cookie persistence.

### DevOps
*   **Containerization:** Ready-to-use `Dockerfile` and `docker-compose` setup for the entire stack.
*   **Tooling:** Includes initialization scripts to rename the project and set up ports automatically.

## Prerequisites

Before you begin, ensure you have the following installed:

*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   [Node.js 22+](https://nodejs.org/) (for local frontend development)
*   [Git](https://git-scm.com/)

## Getting Started

Follow these simple steps to set up your new project:

### 1. Clone the Repository

Fork this repository or clone it directly to your local machine:

```bash
git clone <your-repo-url>
cd web-api-template
```

### 2. Run the Initialization Script

This template includes scripts to rename the project (from "MyProject" to your desired name) and configure ports. It will also restore local .NET tools (like `dotnet-ef`).

**For macOS / Linux:**

```bash
chmod +x init.sh
./init.sh
```

**For Windows (PowerShell):**

```powershell
.\init.ps1
```

**What the script does:**
1.  Asks for your **Project Name** (e.g., `MyAwesomeApi`).
2.  Asks for a **Base Port** (default `13000`).
    *   **Frontend:** `Base Port` (e.g., `13000`).
    *   **API:** `Base Port + 2` (e.g., `13002`).
    *   **Database:** `Base Port + 4` (e.g., `13004`).
3.  Renames all files, directories, and namespaces in the solution.
4.  Updates `docker-compose.local.yml` and configuration files with the new ports.
5.  Restores local .NET tools (ensures `dotnet-ef` is available).

### 3. Run the Application

Once initialized, you can start the entire infrastructure (Frontend + API + Database) using Docker Compose:

```bash
docker compose -f docker-compose.local.yml up -d --build
```

*   **Frontend:** `http://localhost:<BASE_PORT>` (e.g., `http://localhost:13000`)
*   **API:** `http://localhost:<API_PORT>` (e.g., `http://localhost:13002`)
*   **Swagger UI:** `http://localhost:<API_PORT>/scalar/v1`

## Project Structure

```
src/
├── backend/                # .NET Web API solution
│   ├── MyProject.Domain/   # Core domain entities, value objects
│   ├── MyProject.Application/ # Application contracts, features
│   ├── MyProject.Infrastructure/ # Implementation (EF Core, etc.)
│   └── MyProject.WebApi/   # API entry point
│
└── frontend/               # SvelteKit application
    ├── src/
    │   ├── lib/
    │   │   ├── api/        # Generated API client
    │   │   ├── components/ # UI components (Shadcn)
    │   │   └── server/     # Server-side config
    │   └── routes/         # File-based routing
    └── static/
```

## Database Migrations

> **Note:** When running the API in the `Development` configuration, the application automatically applies any pending migrations on startup.

If you need to add new migrations:

1.  Ensure the database container is running.
2.  Run the following command from the root directory:

```bash
dotnet ef migrations add <MigrationName> --project src/backend/<YourProjectName>.Infrastructure --startup-project src/backend/<YourProjectName>.WebApi --output-dir Features/Postgres/Migrations
dotnet ef database update --project src/backend/<YourProjectName>.Infrastructure --startup-project src/backend/<YourProjectName>.WebApi
```

## Frontend Development

For a better developer experience (HMR, faster builds), you can run the frontend locally while keeping the backend in Docker:

1.  Start the backend and database:
    ```bash
    docker compose -f docker-compose.local.yml up -d api db
    ```
2.  Navigate to the frontend directory:
    ```bash
    cd src/frontend
    ```
3.  Install dependencies:
    ```bash
    npm install
    ```
4.  Start the dev server:
    ```bash
    npm run dev
    ```

## Localization

The frontend includes a robust localization system built on `Paraglide JS`, designed for high-traffic and scalable SPAs.

### Key Features
*   **Type-Safe Keys:** TypeScript types are generated from the default locale (`en.json`), preventing missing key errors.
*   **Auto-Detection:** Detects language from the browser (client-side) or `Accept-Language` header/cookies (server-side).
*   **Persistence:** User preference is saved in a cookie (`locale`) for consistent experience across sessions.
*   **SSR Support:** Injects the correct `lang` attribute into the HTML tag during server-side rendering to prevent hydration mismatches.

### Adding a New Language
1.  Create a new translation file in `src/frontend/src/lib/locales/` (e.g., `es.json`).
2.  Register the new locale in `src/frontend/src/lib/i18n.ts`:
    ```typescript
    register('es', () => import('./locales/es.json'));
    ```
3.  Add the language code to the `supportedLocales` array in `src/frontend/src/lib/i18n.ts`.

## License

This project is licensed under the [GNU Affero General Public License v3.0 (AGPLv3)](LICENSE).
