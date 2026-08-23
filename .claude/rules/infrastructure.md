---
paths:
  - "src/**"
  - ".github/**"
  - "docker-compose*"
---

# Infrastructure Rules

Extends CLAUDE.md with Aspire, NuGet, and Docker patterns.

## Aspire (Local Dev)
- Run: `dotnet run --project src/backend/MyProject.AppHost`
- Launches PostgreSQL, MinIO, MailPit, API, and Frontend
- Logging: Serilog bridges to OTEL via `writeToProviders: true` - do NOT add `Serilog.Sinks.OpenTelemetry` (causes duplicate logs)

## NuGet
- To add: `<PackageVersion Include="Pkg" Version="X.Y.Z" />` in `Directory.Packages.props`, `<PackageReference Include="Pkg" />` in `.csproj`

## Docker Security
- Read-only root filesystem, no-new-privileges, drop all capabilities
- Secrets in env vars or `.env` - never in code or committed config
- Database credentials never in connection strings in logs
- Health check endpoints must not leak sensitive info

## Options Pattern
- `public sealed class {Name}Options` with `const string SectionName`
- `[Required]` on mandatory fields, `string.Empty` default for required strings
- Register with `BindConfiguration`, `ValidateDataAnnotations`, `ValidateOnStart`
