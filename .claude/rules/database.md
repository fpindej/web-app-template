---
paths:
  - "src/backend/**"
---

# Database Rules

Extends CLAUDE.md with EF Core patterns and commands.

## Entity Configuration
- Inherit `BaseEntityConfiguration<T>` (public abstract), override `ConfigureEntity`
- Derived configurations are `internal` - auto-discovered via `ApplyConfigurationsFromAssembly()`
- Default `public` schema - named schemas only for existing grouped features (e.g., `"auth"`)
- `.HasComment()` on all enum columns documenting values
- Boolean naming: `Is*`/`Has*` in C#, prefix-free column names via `HasColumnName`

## Migrations
- Command: `dotnet ef migrations add {Name} --project src/backend/MyProject.Infrastructure --startup-project src/backend/MyProject.WebApi --output-dir Persistence/Migrations`
- Seeding: roles and their default permissions via `AppRoles.Definitions` (declarative, idempotent upsert in `SeedRolesAsync()`)

## Repository Pattern
- `IBaseEntityRepository<T>` for CRUD with automatic soft-delete filtering (global query filter)
- Custom repos: extend `IBaseEntityRepository<T>` in Application, implement in Infrastructure
- Never expose `IQueryable` across layer boundaries
- Pagination: `Paginate(pageNumber, pageSize)` extension on `IQueryable<T>`

## Query Rules
- No string interpolation in LINQ/SQL queries - parameterized only
- `IReadOnlyList<T>` on public interfaces - never expose `List<T>` or `T[]`
- `HybridCache` for caching with keys from `CacheKeys` constants
