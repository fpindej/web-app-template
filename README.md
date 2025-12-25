# Web API Template

This repository serves as a robust starting point for building .NET Web APIs. It comes pre-configured with essential components to jumpstart your development, following Clean Architecture principles.

## Features

*   **Clean Architecture:** Organized into Domain, Application, Infrastructure, and WebApi layers.
*   **Database:** Pre-configured PostgreSQL connection with Entity Framework Core.
*   **Identity:** Built-in authentication system (JWT/Cookie-based).
*   **Containerization:** Ready-to-use `Dockerfile` and `docker-compose` setup.
*   **Tooling:** Includes initialization scripts to rename the project and set up ports automatically.

## Prerequisites

Before you begin, ensure you have the following installed:

*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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
    *   API will run on `Base Port + 2` (e.g., `13002`).
    *   Database will run on `Base Port + 4` (e.g., `13004`).
3.  Renames all files, directories, and namespaces in the solution.
4.  Updates `docker-compose.local.yml` and configuration files with the new ports.
5.  Restores local .NET tools (ensures `dotnet-ef` is available).
6.  (Optional) Creates a fresh initial Entity Framework migration.

### 3. Run the Application

Once initialized, you can start the entire infrastructure (API + Database) using Docker Compose:

```bash
docker compose -f docker-compose.local.yml up -d
```

The API will be available at `http://localhost:<API_PORT>` (e.g., `http://localhost:13002`).
The Swagger UI can be accessed at `http://localhost:<API_PORT>/swagger` (if configured in development).

## Project Structure

*   **src/MyProject.Domain**: Contains enterprise logic and entities.
*   **src/MyProject.Application**: Contains application logic, interfaces, and DTOs.
*   **src/MyProject.Infrastructure**: Contains implementation of interfaces (EF Core, external services).
*   **src/MyProject.WebApi**: The entry point of the application (Controllers, Middleware).

## Database Migrations

> **Note:** When running the API in the `Development` configuration, the application automatically applies any pending migrations on startup.

If you chose not to run migrations during initialization, or need to add new ones later:

1.  Ensure the database container is running.
2.  Restore local tools (if you haven't already):
    ```bash
    dotnet tool restore
    ```
3.  Run the following command from the root directory:

```bash
dotnet ef migrations add <MigrationName> --project src/<YourProjectName>.Infrastructure --startup-project src/<YourProjectName>.WebApi --output-dir Features/Postgres/Migrations
dotnet ef database update --project src/<YourProjectName>.Infrastructure --startup-project src/<YourProjectName>.WebApi
```

## License

[MIT](LICENSE)
