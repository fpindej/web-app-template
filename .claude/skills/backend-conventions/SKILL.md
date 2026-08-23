---
description: "Backend convention reference (.NET 10 / C# 13). Auto-injected into backend-aware agents - not user-invocable."
user-invocable: false
---

# Backend Conventions (.NET 10 / C# 13)

## Project Structure

```
src/backend/
├── MyProject.Shared/              # Result, Error, ErrorType, ErrorMessages (zero deps)
├── MyProject.Domain/Entities/     # Business entities (BaseEntity)
├── MyProject.Application/         # Interfaces, DTOs, service contracts
│   ├── Features/{Feature}/I{Feature}Service.cs
│   ├── Features/{Feature}/Dtos/{Operation}Input.cs, {Entity}Output.cs
│   ├── Identity/IUserService.cs, IUserContext.cs
│   └── Identity/Constants/AppRoles.cs, AppPermissions.cs
├── MyProject.Infrastructure/      # Implementations (all internal)
│   ├── Features/{Feature}/Services/, Configurations/, Extensions/
│   └── Persistence/MyProjectDbContext.cs
├── MyProject.ServiceDefaults/     # Aspire shared: OTEL, service discovery, resilience
├── MyProject.AppHost/             # Aspire orchestrator (local dev only)
└── MyProject.WebApi/              # Entry point
    ├── Features/{Feature}/{Feature}Controller.cs, {Feature}Mapper.cs
    ├── Features/{Feature}/Dtos/{Operation}/{Operation}Request.cs + Validator
    ├── Authorization/             # RequirePermission, PermissionPolicyProvider
    └── Shared/                    # ApiController, ProblemFactory, ValidationConstants
```

## C# Conventions

### Access Modifiers

| Item | Modifier |
|---|---|
| Domain entities, Application interfaces/DTOs | `public` |
| Infrastructure services, EF configs, mappers | `internal` |
| WebApi controllers, request/response DTOs | `public` |

### Key Rules

- **Nullability**: `string.Empty` for required, `string?` for optional. Express nullability in the type system.
- **Collections**: Prefer `IReadOnlyList<T>` on public interfaces. Avoid exposing `List<T>` or `T[]` directly.
- **Time**: `TimeProvider` registered as `TimeProvider.System` singleton.
- **NuGet**: To add a package: `<PackageVersion Include="Pkg" Version="X.Y.Z" />` in `Directory.Packages.props`, `<PackageReference Include="Pkg" />` in `.csproj`.

## Entity Definition

New entities should extend `BaseEntity` (provides `Id`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By` - all set by `AuditingInterceptor` automatically) and use `BaseEntityRepository<T>` for data access.

Rules:
- Private setters, enforce invariants through methods
- Protected parameterless ctor for EF Core
- Derived entity ctor sets `Id = Guid.NewGuid()` (via `protected init`)
- Boolean naming: `Is*`/`Has*` in C#, prefix-free column names via `HasColumnName`
- Soft delete via `entity.SoftDelete()` / `entity.Restore()` - never set `IsDeleted` directly

## Audit Trail

`AuditEvent` is append-only, does NOT extend `BaseEntity`. No FK on `UserId` (users are hard-deleted). Fire-and-forget logging - failures never break operations.

```csharp
await auditService.LogAsync(AuditActions.AdminAssignRole, userId: callerId,
    targetEntityType: "User", targetEntityId: targetId,
    metadata: JsonSerializer.Serialize(new { role = input.Role }), ct: cancellationToken);
```

Always serialize metadata with `JsonSerializer.Serialize` - never string interpolation (JSON injection risk).

## EF Core

Configurations inherit `BaseEntityConfiguration<T>` (`public abstract`), override `ConfigureEntity`. Mark derived configurations `internal`. Auto-discovered via `ApplyConfigurationsFromAssembly()`.

- Default `public` schema. Named schemas only for existing grouped features (e.g., `"auth"`).
- `.HasComment()` on all enum columns documenting values.
- Seeding: roles and their default permissions via `AppRoles.Definitions` (declarative, idempotent upsert in `SeedRolesAsync()`).

Migration command:
```bash
dotnet ef migrations add {Name} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Persistence/Migrations
```

## Result Pattern

```csharp
// Success
return Result<Guid>.Success(entity.Id);

// Static error (code + message) - always an ErrorMessages entry
return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);

// Runtime values go in server-side logs, never in client responses
logger.LogWarning("Operation failed for user '{UserId}': {Errors}", userId, errors);
return Result.Failure(ErrorMessages.Admin.DeleteFailed);
```

| ErrorType | HTTP | When |
|---|---|---|
| *(omit - default)* | 400 | Validation / business rule failures |
| `ErrorType.Unauthorized` | 401 | Auth / token failures |
| `ErrorType.Forbidden` | 403 | Authenticated but insufficient privileges |
| `ErrorType.NotFound` | 404 | Entity not found |

Controller: `ProblemFactory.Create(result.Error, result.ErrorType)` for failures. `result.Error` is an `Error` record (`Code`, `Message`); the factory writes `Message` to `detail` and `Code` to the `code` extension.

## Service Pattern

1. **Application**: `public interface I{Feature}Service` + `record` DTOs (Input/Output)
2. **Infrastructure**: `internal class {Feature}Service(deps...) : I{Feature}Service` - primary ctor, `IOptions<T>` for config
3. **DI extension**: C# 13 `extension(IServiceCollection)` syntax, called from `Program.cs`

## DTO Naming

| Layer | Pattern | Example |
|---|---|---|
| WebApi Request | `{Operation}Request` | `LoginRequest` |
| WebApi Response | `{Entity}Response` | `UserResponse` |
| Application Input | `{Operation}Input` | `RegisterInput` |
| Application Output | `{Entity}Output` | `UserOutput` |

Mappers: `internal static class {Feature}Mapper` with extension methods (`request.ToInput()`, `output.ToResponse()`).

WebApi response DTOs: classes with `init` properties and `[UsedImplicitly]` from JetBrains.Annotations.

## Controller Conventions

- Authenticated endpoints extend `ApiController` (`[Authorize]`, route `api/v1/[controller]`)
- Public endpoints use `ControllerBase` directly (route `api/[controller]`)
- Include `/// <summary>`, `[ProducesResponseType]` per status code, and `CancellationToken` as last param
- Never `/// <param name="cancellationToken">` - it leaks into OAS `requestBody.description`
- File uploads: `[FromForm]` with `IFormFile`, `[Consumes("multipart/form-data")]`, `[RequestSizeLimit(bytes)]`
- Error responses: use `ProblemFactory.Create()` - avoid `NotFound()`, `BadRequest()`, or anonymous objects
- Success responses: `Ok(response)`, `Created(string.Empty, response)`
- `[ProducesResponseType]` without `typeof(...)` on error codes (400, 401, 403, 404, 429)

## Validation

FluentValidation auto-discovered from WebApi assembly. Co-locate validators with request DTOs.

| Rule Type | Convention |
|---|---|
| New passwords | `MinimumLength(6)` + lowercase + uppercase + digit rules |
| Existing passwords | `NotEmpty()` + `MaximumLength(255)` only |
| Optional fields | `.When(x => !string.IsNullOrEmpty(x.Field))` |
| URL fields | `Uri.TryCreate` + restrict to `http`/`https` schemes |
| Shared patterns | Extract to `ValidationConstants.cs` |

## Error Messages and Codes

- Client-facing errors are centralized as `static readonly Error` entries in `ErrorMessages.cs` nested classes. `Error(Code, Message)` pairs a stable, machine-readable snake_case code with the human-readable message.
- Codes are derived from the declaring location: `{nested_class}_{field_name}` in snake_case (`ErrorMessages.ExternalAuth.StateExpired` -> `external_auth_state_expired`). `ErrorMessagesTests` enforces the pattern and global uniqueness.
- Every `ProblemDetails` response carries the code in the `code` extension (`ProblemFactory` for controllers/middleware, `ProblemFactory.EnsureCode` in `AddProblemDetails` for framework-generated bodies: `validation_failed` for model validation, snake_case reason phrase such as `not_found` otherwise). `ProblemDetailsSchemaTransformer` documents it in OpenAPI.
- Codes are a public contract: adding one is additive, renaming or removing one is a breaking change for API consumers (frontend maps on codes, not on `detail` text).
- Runtime values (role names, user IDs, framework errors): log server-side via `ILogger`, never in `Result.Failure()`
- Identity errors: log the error `.Code` values server-side, return a static `ErrorMessages` entry to the client - never echo `.Description` (it can embed emails and enables account enumeration). No exceptions: register, change-password, and reset-password all return static messages; client-side validators provide the detailed password-policy feedback.
- To add: create `public static readonly Error X = new("{class}_{x}", "...")` in the matching `ErrorMessages.cs` nested class. Dynamic values go in logs, not in Result.

## Authorization

### Permission System

Atomic permissions via `[RequirePermission("permission.name")]` on controller actions. Permissions stored as role claims, embedded in JWT as `"permission"` claims.

- `AppPermissions.cs`: constants discovered via reflection (`AppPermissions.All`)
- `PermissionAuthorizationHandler`: Superuser bypass -> claim match -> deny
- Never class-level `[Authorize(Roles)]` on controllers using permissions
- To add a role: add `public const string` to `AppRoles.cs` - reflection discovers it, seeding picks it up automatically.

### Role Hierarchy

`Superuser` (3) > `Admin` (2) > `User` (1) > Custom (0). Enforced by Admin service:
- Cannot manage users at/above your rank
- Cannot assign/remove roles at/above your rank
- Cannot modify your own roles, lock yourself, or delete yourself

Permission changes on a role -> invalidate refresh tokens + rotate security stamps + clear cache for all affected users.

## Repository Pattern

`IBaseEntityRepository<T>` provides CRUD with automatic soft-delete filtering (global query filter). Open generic registration covers basic entities.

Custom repositories: extend `IBaseEntityRepository<T>` in Application, implement in Infrastructure with `BaseEntityRepository<T>`. Avoid exposing `IQueryable` across layer boundaries.

Pagination: `Paginate(int pageNumber, int pageSize)` extension on `IQueryable<T>` returns `IQueryable<T>` (applies `Skip`/`Take`).

## Caching

`HybridCache` (.NET built-in) provides L1 in-process caching with stampede protection. Keys defined in `CacheKeys` constants. `UserCacheInvalidationInterceptor` auto-clears user cache on entity changes.

## File Storage

`IFileStorageService` - generic S3-compatible interface (`Upload`, `Download`, `Delete`, `Exists`). Implementation: `S3FileStorageService` (works with MinIO locally, any S3-compatible provider in production).

**Uploading files from a controller:**
1. Accept `IFormFile` via `[FromForm]` + `[Consumes("multipart/form-data")]` + `[RequestSizeLimit]`
2. Read to `byte[]` in the controller: `using var ms = new MemoryStream(); await file.CopyToAsync(ms); var data = ms.ToArray();`
3. Pass to the service for validation/processing
4. Store via `fileStorageService.UploadAsync(key, data, contentType, ct)` - returns `Result`

**Storage keys:** Use `{feature}/{id}.{ext}` pattern (e.g., `avatars/{userId}.webp`).

## Email Templates

Fluid (Liquid) templates with 3-file pattern (`{name}.liquid`, `{name}.subject.liquid`, `{name}.text.liquid`). See the `/add-email-template` skill for full template patterns and model records.

## OpenAPI

- `/// <summary>` on every controller action and DTO property -> generates OAS descriptions
- `[ProducesResponseType]` declares all possible status codes per action
- `EnumSchemaTransformer` auto-documents enum values
- Scalar UI at `/scalar/v1` (development only)

## Options Pattern

```csharp
public sealed class {Name}Options
{
    public const string SectionName = "{Section}";

    /// <summary>Gets or sets the ...</summary>
    [Required]
    public string Value { get; init; } = string.Empty;
}
```

Register with `BindConfiguration`, `ValidateDataAnnotations`, `ValidateOnStart`.

## Testing

| Project | Tests | Dependencies |
|---|---|---|
| `Unit.Tests` | Pure logic (Shared, Domain, Application) | None - no mocks, no DI |
| `Component.Tests` | Service business logic | `TestDbContextFactory` (InMemory), `NSubstitute`, `IdentityMockHelpers` |
| `Api.Tests` | Full HTTP pipeline (routes, auth, status codes) | `CustomWebApplicationFactory`, `TestAuthHandler` |
| `Architecture.Tests` | Layer deps, naming, visibility | NetArchTest |

API test auth: `"Authorization", "Test"` (basic user), `TestAuth.WithPermissions(...)` (specific perms), `TestAuth.Superuser()`.

Response contracts: frozen records in `Contracts/ResponseContracts.cs` - deserialize and assert key fields.

## Aspire (Local Development)

Run: `dotnet run --project src/backend/MyProject.AppHost` - launches PostgreSQL, MinIO, MailPit, API, and Frontend. See `/add-aspire-dep` skill for adding dependencies.

**Logging gotcha**: Serilog bridges to OTEL via `writeToProviders: true` - do NOT add `Serilog.Sinks.OpenTelemetry` (causes duplicate logs).
