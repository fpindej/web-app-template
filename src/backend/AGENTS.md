# Backend Conventions (.NET 10 / C# 13)

> Follow the **Agent Workflow** in the root [`AGENTS.md`](../../AGENTS.md) — commit atomically, run `dotnet build` before each commit, and write session docs when asked.

## Project Structure

```
src/backend/
├── MyProject.Domain/              # Entities, value objects, Result pattern
│   ├── Entities/
│   │   └── BaseEntity.cs
│   └── Result.cs
│
├── MyProject.Application/         # Interfaces and DTOs (contracts only)
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── I{Feature}Service.cs
│   │       └── Dtos/
│   │           ├── {Operation}Input.cs
│   │           └── {Entity}Output.cs
│   ├── Persistence/
│   │   ├── IBaseEntityRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Caching/
│   │   ├── ICacheService.cs
│   │   └── Constants/CacheKeys.cs
│   ├── Cookies/
│   │   └── ICookieService.cs
│   └── Identity/
│       ├── IUserContext.cs
│       └── IUserService.cs
│
├── MyProject.Infrastructure/      # Implementations
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── Services/          # Service implementations
│   │       ├── Models/            # EF/Identity models
│   │       ├── Configurations/    # IEntityTypeConfiguration
│   │       ├── Extensions/        # DI registration
│   │       ├── Options/           # Configuration binding classes
│   │       └── Constants/         # Feature-specific constants
│   ├── Persistence/
│   │   ├── MyProjectDbContext.cs
│   │   ├── BaseEntityRepository.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/        # Shared EF configs (BaseEntityConfiguration)
│   │   ├── Extensions/            # Query helpers, migrations, pagination
│   │   └── Interceptors/          # AuditingInterceptor, cache invalidation
│   ├── Caching/
│   ├── Cookies/
│   ├── Identity/
│   └── Logging/
│
└── MyProject.WebApi/              # API entry point
    ├── Program.cs
    ├── Features/
    │   └── {Feature}/
    │       ├── {Feature}Controller.cs
    │       ├── {Feature}Mapper.cs
    │       └── Dtos/
    │           └── {Operation}/
    │               ├── {Operation}Request.cs
    │               └── {Operation}RequestValidator.cs
    ├── Shared/                    # ApiController, ErrorResponse, PaginatedRequest/Response
    ├── Middlewares/                # ExceptionHandlingMiddleware
    ├── Extensions/                # CORS, rate limiting
    └── Options/                   # CorsOptions, RateLimitingOptions
```

## C# Conventions

### Access Modifiers — Minimal Scope

Always use the **most restrictive** access modifier that works:

| Modifier | Use When |
|---|---|
| `private` | Only used within the same class (default for fields, helpers) |
| `protected` | Needed by derived classes (e.g., EF Core parameterless constructors) |
| `internal` | Used within the same assembly but not exposed outside (service implementations, mappers, EF configs) |
| `public` | Part of the assembly's public API (interfaces, DTOs, controllers, domain entities) |

Quick reference by layer:

| Item | Modifier | Why |
|---|---|---|
| Domain entities | `public` | Referenced by all layers |
| Application interfaces | `public` | Consumed across assemblies |
| Application DTOs | `public` | Passed across layer boundaries |
| Infrastructure services | `internal` | Only exposed via their interface |
| Infrastructure EF configs | `internal` | Auto-discovered, never referenced directly |
| WebApi controllers | `public` | ASP.NET Core requires it for routing |
| WebApi mappers | `internal` | Only used within WebApi assembly |
| WebApi request/response DTOs | `public` | Serialized by framework |

### Nullable Reference Types

Nullable reference types are **enabled project-wide** (`<Nullable>enable</Nullable>` in `Directory.Build.props`). Be explicit and intentional:

```csharp
// ✅ Explicit nullability — make intent clear
public string Email { get; init; } = string.Empty;    // Required — never null
public string? PhoneNumber { get; init; }              // Optional — may be null
public Task<TEntity?> GetByIdAsync(Guid id, ...);     // May not exist

// ❌ Wrong — lazy defaults that hide intent
public string Email { get; init; } = null!;            // Lying to the compiler
public string Email { get; init; }                     // Warning: uninitialized
```

Rules:
- **`string` properties** → initialize with `string.Empty` if required, mark `string?` if optional
- **Return types** → use `T?` when the value legitimately might not exist (e.g., `GetByIdAsync` returns `TEntity?`)
- **Parameters** → use `T?` for optional parameters, `T` for required ones
- **Never use `null!`** (the null-forgiving operator) — it defeats the purpose of NRT. If you need it, the design is wrong.
- **DTOs**: match nullability to whether the field is required in the API contract — this flows through to the OpenAPI spec and generated TypeScript types

## Entity Definition

All domain entities extend `BaseEntity`, which provides audit fields and soft delete:

```csharp
// BaseEntity provides these fields automatically:
public Guid Id { get; protected init; }
public DateTime CreatedAt { get; private init; }      // Set by AuditingInterceptor
public Guid? CreatedBy { get; private init; }          // Set by AuditingInterceptor
public DateTime? UpdatedAt { get; private set; }       // Set by AuditingInterceptor
public Guid? UpdatedBy { get; private set; }           // Set by AuditingInterceptor
public bool IsDeleted { get; private set; }            // Managed by SoftDelete()/Restore()
public DateTime? DeletedAt { get; private set; }       // Set by AuditingInterceptor
public Guid? DeletedBy { get; private set; }           // Set by AuditingInterceptor
```

The `AuditingInterceptor` **automatically** populates `CreatedAt`/`CreatedBy` on insert, `UpdatedAt`/`UpdatedBy` on update, and `DeletedAt`/`DeletedBy` on soft delete — never set these manually.

### Creating a New Entity

```csharp
// Domain/Entities/Order.cs
public class Order : BaseEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    protected Order() { } // EF Core constructor — always required

    public Order(string orderNumber, decimal totalAmount)
    {
        Id = Guid.NewGuid();
        OrderNumber = orderNumber;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    public void Deliver() => Status = OrderStatus.Delivered;
}
```

Key rules:
- **Private setters** on all properties — enforce invariants through methods
- **Protected parameterless constructor** for EF Core materialization
- **Public constructor** for domain creation with required parameters
- **Generate `Id`** in the constructor

## EF Core Configuration

Configurations inherit from `BaseEntityConfiguration<T>`, which handles all `BaseEntity` fields (primary key, audit columns, soft delete index, and a global query filter that excludes soft-deleted entities). Override `ConfigureEntity` to add entity-specific mapping:

```csharp
// Infrastructure/Features/Orders/Configurations/OrderConfiguration.cs
internal class OrderConfiguration : BaseEntityConfiguration<Order>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasComment("OrderStatus enum: 0=Pending, 1=Processing, 2=Shipped, 3=Delivered, 4=Cancelled");
        builder.HasIndex(e => e.OrderNumber).IsUnique();
    }
}
```

Configurations are auto-discovered via `modelBuilder.ApplyConfigurationsFromAssembly()` in `MyProjectDbContext`.

After creating entity + configuration:
1. Add `DbSet<Order>` to `MyProjectDbContext`
2. Run migration:

```bash
dotnet ef migrations add AddOrder \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Features/Postgres/Migrations
```

## Result Pattern

Use `Result` / `Result<T>` for all operations that can fail expectedly. Never throw exceptions for business logic failures.

```csharp
// Success
return Result<Guid>.Success(entity.Id);
return Result.Success();

// Failure
return Result<Guid>.Failure("Email already exists.");
return Result.Failure("Invalid credentials.");
```

In controllers, map Result to HTTP responses:

```csharp
var result = await service.CreateAsync(input, cancellationToken);
if (!result.IsSuccess)
    return BadRequest(new ErrorResponse { Message = result.Error });
return CreatedAtAction(nameof(Get), new { id = result.Value });
```

## Service Composition

### 1. Define Interface (Application Layer)

```csharp
// Application/Features/Authentication/IAuthenticationService.cs
public interface IAuthenticationService
{
    Task<Result> Login(string username, string password, CancellationToken cancellationToken = default);
    Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default);
    Task Logout(CancellationToken cancellationToken = default);
    Task<Result> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
```

### 2. Define DTOs (Application Layer)

Use **records** for Application-layer DTOs:

```csharp
// Application/Features/Authentication/Dtos/RegisterInput.cs
public record RegisterInput(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);

// Application/Features/Authentication/Dtos/UserOutput.cs
public record UserOutput(
    Guid Id,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl,
    IEnumerable<string> Roles
);
```

### 3. Implement Service (Infrastructure Layer)

Use **primary constructors** for dependency injection:

```csharp
// Infrastructure/Features/Authentication/Services/AuthenticationService.cs
internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    ITokenProvider tokenProvider,
    ICookieService cookieService,
    IOptions<AuthenticationOptions> authenticationOptions,
    MyProjectDbContext dbContext,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;

    public async Task<Result<Guid>> Register(RegisterInput input)
    {
        var user = new ApplicationUser { UserName = input.Email, Email = input.Email };
        var identityResult = await userManager.CreateAsync(user, input.Password);

        if (!identityResult.Succeeded)
            return Result<Guid>.Failure(identityResult.Errors.First().Description);

        return Result<Guid>.Success(user.Id);
    }
}
```

Key rules:
- Mark implementations as `internal`
- Use `IOptions<T>` for configuration, extract `.Value` to a readonly field
- Primary constructor parameters are the injected dependencies

### 4. Register in DI (Infrastructure Layer)

Use the **C# 13 extension member syntax**:

```csharp
// Infrastructure/Features/Authentication/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentity<TContext>(IConfiguration configuration)
            where TContext : DbContext
        {
            // Identity configuration...
            services.AddScoped<ITokenProvider, JwtTokenProvider>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            return services;
        }
    }
}
```

Then call from `Program.cs` (typically via a wrapper extension that calls this internally):

```csharp
builder.Services.AddIdentityServices(builder.Configuration);
```

## DTO Naming & Mapping

| Layer | Pattern | Example |
|---|---|---|
| WebApi Request | `{Operation}Request` | `LoginRequest`, `RegisterRequest`, `UpdateUserRequest` |
| WebApi Response | `{Entity}Response` | `UserResponse` |
| Application Input | `{Operation}Input` | `RegisterInput`, `UpdateProfileInput` |
| Application Output | `{Entity}Output` | `UserOutput` |

### Mapper Pattern

Create static mapper classes in the WebApi layer using extension methods:

```csharp
// WebApi/Features/Authentication/AuthMapper.cs
internal static class AuthMapper
{
    public static RegisterInput ToRegisterInput(this RegisterRequest request) =>
        new(
            Email: request.Email,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber
        );
}

// WebApi/Features/Users/UserMapper.cs
internal static class UserMapper
{
    public static UserResponse ToResponse(this UserOutput user) => new()
    {
        Id = user.Id,
        Username = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Bio = user.Bio,
        AvatarUrl = user.AvatarUrl,
        Roles = user.Roles
    };
}
```

### WebApi Response DTOs

Use classes with `init` properties and `[UsedImplicitly]` from JetBrains.Annotations:

```csharp
public class UserResponse
{
    public Guid Id { [UsedImplicitly] get; [UsedImplicitly] init; }
    public string Username { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;
    public string Email { [UsedImplicitly] get; init; } = string.Empty;
    public string? FirstName { [UsedImplicitly] get; init; }
    // ...
}
```

## Controller Conventions

### Authorized Endpoints — Extend `ApiController`

```csharp
// Shared/ApiController.cs — base for all authorized, versioned controllers
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public abstract class ApiController : ControllerBase;
```

### Public Endpoints — Use `ControllerBase` Directly

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and sets HttpOnly cookie tokens.
    /// </summary>
    /// <param name="request">The login credentials</param>
    /// <response code="200">Returns success (tokens set in HttpOnly cookies)</response>
    /// <response code="401">If the credentials are invalid</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Login(request.Username, request.Password, cancellationToken);
        if (!result.IsSuccess)
            return Unauthorized(new ErrorResponse { Message = result.Error });
        return Ok();
    }
}
```

Rules:
- Always include `/// <summary>` XML docs — these generate OpenAPI descriptions consumed by the frontend
- Always include `[ProducesResponseType]` for all possible status codes — with `typeof(ErrorResponse)` on error codes that return a body (400, 401, etc.)
- Always accept `CancellationToken` as the last parameter on async endpoints
- **Never add `/// <param name="cancellationToken">`** — ASP.NET excludes `CancellationToken` from OAS parameters, but the `<param>` text leaks into `requestBody.description`. CS1573 is suppressed project-wide.
- Only add `/// <param>` tags for parameters that should appear in the OAS (request body, route/query params)
- **Never return anonymous objects or raw strings** from controllers — always use a defined DTO (`ErrorResponse` for errors, typed response DTOs for success). Anonymous objects produce untyped schemas in the OAS.
- **Never use `#pragma warning disable`** for XML doc warnings — fix the docs instead
- Use primary constructors for dependency injection
- Map requests to inputs via mapper extension methods: `request.ToRegisterInput()`

## Validation

FluentValidation validators are **auto-discovered** from the WebApi assembly (`AddValidatorsFromAssemblyContaining<Program>()`). Co-locate validators with their request DTOs:

```csharp
// WebApi/Features/Authentication/Dtos/Register/RegisterRequestValidator.cs
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
```

Simple DTOs can also use data annotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`, etc.) — both validation systems work together.

## Error Handling

Three-tier strategy:

1. **Expected business failures** → Return `Result.Failure("message")` from services
2. **Not found** → Throw `KeyNotFoundException` → `ExceptionHandlingMiddleware` returns 404
3. **Pagination errors** → Throw `PaginationException` → middleware returns 400
4. **Unexpected errors** → Let them propagate → middleware returns 500 with `ErrorResponse`

```csharp
// ExceptionHandlingMiddleware catches and maps exceptions:
// KeyNotFoundException     → 404 (logged as Warning)
// PaginationException      → 400 (logged as Warning)
// Everything else          → 500 (logged as Error, stack trace in Development only)
```

The `ErrorResponse` shape:

```csharp
public class ErrorResponse
{
    public string? Message { get; init; }
    public string? Details { get; init; } // Stack trace — Development only
}
```

`ErrorResponse` is the **only** error body type across the entire API — both controllers and middleware return it. The middleware serializes with explicit `JsonNamingPolicy.CamelCase` to match ASP.NET's controller serialization. Never return raw strings, anonymous objects, or other shapes for errors.

## Repository & Unit of Work

`IBaseEntityRepository<T>` provides standard CRUD with automatic soft-delete filtering:

```csharp
public interface IBaseEntityRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, bool asTracking = false, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(int pageNumber, int pageSize, bool asTracking = false, CancellationToken ct = default);
    Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken ct = default);
    void Update(TEntity entity);
    Task<Result<TEntity>> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<TEntity>> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
```

All queries automatically exclude soft-deleted records via a global EF Core query filter (`HasQueryFilter(e => !e.IsDeleted)`) configured in `BaseEntityConfiguration`. Use `.IgnoreQueryFilters()` when you need to query deleted entities (e.g., `RestoreAsync`). Use `IUnitOfWork` for explicit save and transaction control:

```csharp
await repository.AddAsync(entity, ct);
await unitOfWork.SaveChangesAsync(ct);
```

For transactions spanning multiple operations:

```csharp
await unitOfWork.BeginTransactionAsync(ct);
// ... multiple repository operations ...
await unitOfWork.CommitTransactionAsync(ct);
```

## Pagination

Use the shared abstract base classes for paginated endpoints:

- `PaginatedRequest` — `PageNumber` (default 1, min 1) and `PageSize` (default 10, max 100)
- `PaginatedResponse<T>` — `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`

The `PaginationExtensions.Paginate()` extension method applies Skip/Take with validation and caps page size at 100.

## Caching

`ICacheService` wraps Redis with JSON serialization:

```csharp
// Cache-aside pattern
var user = await cacheService.GetOrSetAsync(
    CacheKeys.User(userId),
    async ct => await FetchUserFromDb(userId, ct),
    CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(1)),
    cancellationToken
);
```

Cache keys are defined in `Application/Caching/Constants/CacheKeys.cs` as static methods (e.g., `CacheKeys.User(userId)` → `"user:{guid}"`).

The `UserCacheInvalidationInterceptor` automatically invalidates user cache entries when `ApplicationUser` entities are modified in the DbContext — no manual invalidation needed for user data.

## Options Pattern

Configuration classes use the **Options pattern** with Data Annotations + `IValidatableObject` for validation. All options are validated at startup via `ValidateDataAnnotations()` + `ValidateOnStart()`.

### Defining Options Classes

Options classes are `public sealed class` with `const string SectionName` matching the `appsettings.json` path. **The class name must correspond to the closest related parent section** — e.g., if the section is `Authentication:Jwt`, the root class is `AuthenticationOptions` (not `JwtOptions`). Properties use `init`-only setters with sensible defaults:

```csharp
// Infrastructure/Features/Authentication/Options/AuthenticationOptions.cs
public sealed class AuthenticationOptions : IValidatableObject
{
    public const string SectionName = "Authentication";

    [Required]
    public JwtOptions Jwt { get; init; } = new();

    // Cross-property and complex validation via IValidatableObject
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Jwt.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "JWT key contains placeholder text.",
                [nameof(Jwt)]);
        }
    }

    public sealed class JwtOptions
    {
        [Required]
        [MinLength(32)]
        public string Key { get; init; } = string.Empty;

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Range(1, 120)]
        public int ExpiresInMinutes { get; init; } = 10;

        public RefreshTokenOptions RefreshToken { get; init; } = new();

        // Nested options — no SectionName, bound automatically via parent
        public sealed class RefreshTokenOptions
        {
            [Range(1, 365)]
            public int ExpiresInDays { get; [UsedImplicitly] init; } = 7;
        }
    }
}
```

### Placement

| Layer | Directory | When |
|---|---|---|
| Infrastructure | `Features/{Feature}/Options/` or `{Feature}/Options/` | Options consumed by Infrastructure services (JWT, caching, etc.) |
| WebApi | `Options/` | Options consumed only at the API layer (CORS, rate limiting) |

### XML Documentation

Every Options class and every property must have `/// <summary>` XML docs. This includes nested child classes and their properties. Follow the same style as `CachingOptions.cs`:

```csharp
/// <summary>
/// Root authentication configuration options.
/// Maps to the "Authentication" section in appsettings.json.
/// </summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Gets or sets the JWT token configuration.
    /// Contains signing key, issuer, audience, and token lifetime settings.
    /// </summary>
    [Required]
    public JwtOptions Jwt { get; init; } = new();

    /// <summary>
    /// Configuration options for JWT token generation and validation.
    /// </summary>
    public sealed class JwtOptions
    {
        /// <summary>
        /// Gets or sets the symmetric signing key for JWT tokens.
        /// Must be at least 32 characters for HMAC-SHA256.
        /// </summary>
        [Required]
        public string Key { get; init; } = string.Empty;
    }
}
```

Rules:
- **Class-level** `/// <summary>` — describe what the options configure and which `appsettings.json` section they map to (for root classes)
- **Property-level** `/// <summary>` — start with "Gets or sets…", describe the purpose, mention defaults and constraints when relevant
- **Nested class** `/// <summary>` — describe what the sub-section configures
- Use the same `Gets or sets` wording as `CachingOptions` for consistency across the codebase

### Child Options (Sub-Sections)

Child options model nested `appsettings.json` sections (e.g., `Authentication:Jwt:RefreshToken`). **Always nest them as `public sealed class` inside the parent.** They have no `SectionName` — they bind automatically through the parent. They are not registered independently with `AddOptions<>`.

```csharp
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Required]
    public GlobalLimitOptions Global { get; init; } = new();

    public sealed class GlobalLimitOptions
    {
        [Range(1, 1000)]
        public int PermitLimit { get; [UsedImplicitly] init; } = 100;

        public TimeSpan Window { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(1);
    }
}
```

When referencing nested types outside the parent (e.g., in method parameters), use the fully-qualified name: `CachingOptions.RedisOptions`.

Use `[UsedImplicitly]` on `init` setters of child options properties that are only set by the configuration binder (e.g., `public int ExpiresInDays { get; [UsedImplicitly] init; } = 7;`).

### Validation Strategy

| Mechanism | Use For |
|---|---|
| **Data Annotations** (`[Required]`, `[MinLength]`, `[Range]`) | Simple property-level constraints |
| **`IValidatableObject.Validate()`** | Cross-property rules, business logic checks, placeholder detection |

**Never** use `IValidateOptions<T>` — keep all validation on the options class itself via `IValidatableObject`. This keeps validation co-located with the configuration it validates.

When a parent options class composes child options that have their own `IValidatableObject.Validate()`, the parent must **delegate** validation to the children:

```csharp
// Parent delegates to children conditionally
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    if (Redis.Enabled)
    {
        foreach (var result in Redis.Validate(validationContext))
            yield return result;
    }
}
```

This is necessary because `ValidateDataAnnotations()` only invokes `Validate()` on the **root** options class — it does not recurse into composed children automatically.

### Registration

Register options in the feature's DI extension using the standard three-call chain:

```csharp
services.AddOptions<AuthenticationOptions>()
    .BindConfiguration(AuthenticationOptions.SectionName)
    .ValidateDataAnnotations()    // Runs [Required], [MinLength], etc. AND IValidatableObject.Validate()
    .ValidateOnStart();           // Fail fast at startup, not on first resolve
```

`ValidateDataAnnotations()` invokes both data annotations **and** `IValidatableObject.Validate()` — no extra registration needed.

Only **root** options classes (those with `SectionName`) are registered. Nested/composed classes bind through the parent.

### Consuming Options

**Runtime injection** — use `IOptions<T>` in services and extract `.Value` to a readonly field. When consuming a child options class, navigate to it from the root:

```csharp
internal class MyService(IOptions<AuthenticationOptions> authenticationOptions) : IMyService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;
}
```

**Startup configuration** — when options are needed during DI registration (before the container is built), read eagerly from `IConfiguration`:

```csharp
// In a DI extension method that receives IConfiguration
var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
    ?? throw new InvalidOperationException("Authentication options are not configured properly.");
var jwtOptions = authOptions.Jwt;
```

This is common for configuring middleware and third-party libraries (CORS policies, Redis connections, JWT bearer setup, rate limiters) that need values at registration time rather than at request time.

**Never** use `IOptionsMonitor<T>` or `IOptionsSnapshot<T>` — all configuration is static and validated once at startup.

## C# 13 Extension Member Syntax

This project uses the new extension member syntax throughout. **Always** use it for new extension methods:

```csharp
// ✅ Correct — C# 13 extension members
public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> ConditionalWhere<TValue>(TValue? condition,
            Expression<Func<T, bool>> predicate) where TValue : struct
            => condition.HasValue ? query.Where(predicate) : query;

        public IQueryable<T> ConditionalWhere(string? condition,
            Expression<Func<T, bool>> predicate)
            => !string.IsNullOrEmpty(condition) ? query.Where(predicate) : query;
    }
}

// ❌ Wrong — old-style static extension methods
public static IQueryable<T> ConditionalWhere<T>(this IQueryable<T> query, ...) => ...
```

## Enum Handling

Enums are **always strings** in the API layer — JSON responses, OpenAPI spec, and generated TypeScript types. In the **database**, enums are stored as **integers** (the EF Core default) for compact storage and index performance, with a `COMMENT ON COLUMN` documenting the valid values.

### Declaration

Define enums in the **Domain** layer alongside the entity that uses them:

```csharp
// Domain/Entities/OrderStatus.cs
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
```

Rules:
- **Always assign explicit integer values** — never rely on implicit ordering. Inserting a member between existing ones would silently shift all subsequent values and corrupt stored data.
- **PascalCase** member names (C# convention) — these become the string values in JSON and the OAS
- Place in `Domain/Entities/` next to the entity, or in `Domain/Enums/` if shared across entities
- Keep enums small and focused — if an enum grows beyond ~10 members, reconsider the modeling

### Runtime JSON Serialization

`JsonStringEnumConverter` is registered globally in `Program.cs` via `AddJsonOptions`:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

This ensures all API responses serialize enums as `"Shipped"` not `2`. This is non-negotiable — never remove this converter.

### OpenAPI Spec

`EnumSchemaTransformer` handles enum schemas in the OAS output. It does three things:

1. Sets `type: string` (not `integer`)
2. **Lists every enum member** in the `enum` array — so consumers see all valid values
3. **Handles nullable enums** (`MyEnum?`) by preserving the null flag → `type: [string, null]`

The transformer uses `Nullable.GetUnderlyingType()` to unwrap nullable enums, so both `OrderStatus` and `OrderStatus?` properties produce correct schemas.

**OAS output for a non-nullable enum:**
```yaml
Status:
  type: string
  enum: [Pending, Processing, Shipped, Delivered, Cancelled]
```

**OAS output for a nullable enum:**
```yaml
Status:
  type:
    - string
    - "null"
  enum: [Pending, Processing, Shipped, Delivered, Cancelled]
```

### Generated TypeScript Types

The frontend runs `npm run api:generate` which produces TypeScript types from the OAS. With the above setup:

```typescript
// Non-nullable enum → union of literal strings
status: "Pending" | "Processing" | "Shipped" | "Delivered" | "Cancelled";

// Nullable enum → union of literal strings + undefined
status?: "Pending" | "Processing" | "Shipped" | "Delivered" | "Cancelled";
```

**No `unknown`, no numeric values, no type pollution.** If you see `unknown` in the generated types for an enum field, something is wrong with the transformer or annotations.

### EF Core Storage

Store enums as **integers** (the default) — no `HasConversion` needed. Add `.HasComment()` to document the valid values in the database schema:

```csharp
builder.Property(e => e.Status)
    .HasComment("OrderStatus enum: 0=Pending, 1=Processing, 2=Shipped, 3=Delivered, 4=Cancelled");
```

The comment format is `EnumTypeName enum: N=Member, ...` — always include the integer value so anyone querying raw data can decode a row without access to the C# source.

This stores a compact `integer` column in PostgreSQL while the comment makes the column self-documenting. The comment generates `COMMENT ON COLUMN orders."Status" IS '...'` in the migration — pure metadata, zero runtime overhead.

Do **not** use `HasConversion<string>()` — string storage bloats row size and indexes for no benefit when the application always works through the enum type.

### Enum Conventions Summary

| Layer | Mechanism | Result |
|---|---|---|
| **Domain** | `public enum OrderStatus { Pending = 0, ... }` | PascalCase members, explicit integer values |
| **JSON serialization** | `JsonStringEnumConverter` in `Program.cs` | `"Shipped"` not `2` |
| **OpenAPI spec** | `EnumSchemaTransformer` | `type: string`, all values in `enum` array |
| **Nullable OpenAPI** | `EnumSchemaTransformer` | `type: [string, null]`, values still listed |
| **TypeScript types** | `openapi-typescript` generation | `"Shipped" \| "Delivered" \| ...` |
| **Database** | Integer (default) + `.HasComment()` | `integer` column, comment documents values |

## OpenAPI Specification — The API Contract

The OpenAPI spec at `/openapi/v1.json` is **the single source of truth** for the entire frontend. Every controller action, every DTO property, every status code directly generates the TypeScript types the frontend consumes via `openapi-typescript`. A sloppy spec means a sloppy frontend. Treat the OAS output as a first-class deliverable.

### Spec Infrastructure

| Component | Location | Purpose |
|---|---|---|
| Spec generation | `AddOpenApiSpecification()` | Registers OAS v1 with transformers |
| Document transformer | `ProjectDocumentTransformer` | Sets API title, version, auth description |
| Document transformer | `CleanupDocumentTransformer` | Strips redundant content types (text/plain, text/json) and HEAD response bodies |
| Operation transformer | `CamelCaseQueryParameterTransformer` | Converts PascalCase query param names to camelCase; propagates missing descriptions |
| Enum transformer | `EnumSchemaTransformer` | String enums with all members listed; handles nullable enums |
| Numeric transformer | `NumericSchemaTransformer` | Ensures numeric types aren't serialized as strings |
| Scalar UI | `/scalar/v1` (dev only) | Interactive API documentation |

### Mandatory Annotations on Every Controller Action

Every endpoint **must** have all of these — no exceptions:

```csharp
/// <summary>
/// Updates the current authenticated user's profile information.
/// </summary>
/// <param name="request">The profile update request</param>
/// <returns>Updated user information</returns>
/// <response code="200">Returns updated user information</response>
/// <response code="400">If the request is invalid</response>
/// <response code="401">If the user is not authenticated</response>
[HttpPatch("me")]
[ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<UserResponse>> UpdateCurrentUser(...)
```

| Annotation | Why It Matters |
|---|---|
| `/// <summary>` | Becomes the operation description in the spec |
| `/// <param>` | Documents request body and route/query parameters — **do not** include `CancellationToken` (its text leaks into `requestBody.description`) |
| `/// <response code="...">` | Documents what each status code means |
| `[ProducesResponseType(typeof(T), StatusCode)]` | Generates the response schema — use `typeof(UserResponse)` for success, `typeof(ErrorResponse)` for errors that return a body |
| `[ProducesResponseType(StatusCode)]` | For status codes with **no body** (204, or 401 when the controller returns bare `Unauthorized()`) |
| `ActionResult<T>` return type | Reinforces the 200-response schema |

### DTO Documentation — Every Property Gets XML Docs

Every property on every request/response DTO must have `/// <summary>`:

```csharp
/// <summary>
/// Represents a request to update the user's profile information.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; [UsedImplicitly] init; }
}
```

These `<summary>` tags become property descriptions in the OAS schema. Without them, the frontend developer (or agent) has no idea what a field means or expects.

### Nullability → Required/Optional in OAS

DTO nullability directly controls `required` in the generated spec:

```csharp
public string Email { get; init; } = string.Empty;  // → required in OAS, non-nullable in TypeScript
public string? FirstName { get; init; }              // → optional in OAS, T | undefined in TypeScript
```

Get this wrong and the frontend types are wrong. See the [Nullable Reference Types](#nullable-reference-types) section.

### Validation Annotations → OAS Constraints

Data annotations on DTOs flow into the spec as schema constraints:

```csharp
[MaxLength(255)]           // → maxLength: 255
[MinLength(6)]             // → minLength: 6
[Range(1, 100)]            // → minimum: 1, maximum: 100
[EmailAddress]             // → format: email
[Required]                 // → required (in addition to non-nullable)
```

Use these alongside FluentValidation — data annotations feed the spec, FluentValidation handles complex rules at runtime.

### OAS Compliance Checklist

Before adding or modifying any endpoint, verify:

- [ ] `/// <summary>` on the controller action describing what it does
- [ ] `/// <param>` for every **visible** parameter (request body, route, query) — **never** for `CancellationToken` (leaks into `requestBody.description`)
- [ ] `/// <response code="...">` for every possible status code
- [ ] `[ProducesResponseType]` for every status code — with `typeof(T)` for response bodies, `typeof(ErrorResponse)` for error codes that return a body
- [ ] `ActionResult<T>` return type (not bare `ActionResult`) when returning a success body
- [ ] Error responses always return `new ErrorResponse { Message = ... }` — never raw strings or anonymous objects
- [ ] `/// <summary>` on every DTO class
- [ ] `/// <summary>` on every DTO property
- [ ] Correct nullability (`string` vs `string?`) matching the API contract
- [ ] Data annotations (`[MaxLength]`, `[Range]`, etc.) on request DTOs for spec constraints
- [ ] `[Description("...")]` on base-class query parameter properties (e.g. `PaginatedRequest`) — XML `<summary>` doesn't flow to inherited OAS parameters
- [ ] `CancellationToken` as the last parameter on all async endpoints, passed through to service calls — **no** `<param>` XML doc for it
- [ ] Route uses lowercase (`[Route("api/[controller]")]` + `LowercaseUrls = true`)
- [ ] Enums serialize as strings with all members listed (handled by `JsonStringEnumConverter` + `EnumSchemaTransformer` — verify in Scalar)
- [ ] No `#pragma warning disable` — CS1573 (partial param docs) is suppressed project-wide because omitting `CancellationToken` param tags is intentional

## Adding a New Feature — Checklist

1. **Domain**: Create entity in `Domain/Entities/` extending `BaseEntity`
2. **Domain**: If the entity has enum properties, define them with explicit integer values in `Domain/Entities/` (or `Domain/Enums/` if shared)
3. **Application**: Define `I{Feature}Service` in `Application/Features/{Feature}/`
4. **Application**: Create Input/Output record DTOs in `Application/Features/{Feature}/Dtos/`
5. **Infrastructure**: Implement service in `Infrastructure/Features/{Feature}/Services/` (mark `internal`)
6. **Infrastructure**: Add EF configuration in `Infrastructure/Features/{Feature}/Configurations/` (extend `BaseEntityConfiguration<T>`) — add `.HasComment()` on enum columns
7. **Infrastructure**: Create DI extension in `Infrastructure/Features/{Feature}/Extensions/ServiceCollectionExtensions.cs`
8. **Infrastructure**: Add `DbSet<Entity>` to `MyProjectDbContext`
9. **WebApi**: Create controller in `WebApi/Features/{Feature}/` (extend `ApiController` or `ControllerBase`)
10. **WebApi**: Create Request/Response DTOs in `WebApi/Features/{Feature}/Dtos/{Operation}/`
11. **WebApi**: Create Mapper in `WebApi/Features/{Feature}/{Feature}Mapper.cs`
12. **WebApi**: Add validators co-located with request DTOs
13. **WebApi**: Wire DI call in `Program.cs`
14. **Migration**: `dotnet ef migrations add ...`

Commit atomically: entity+config → service interface+DTOs → service implementation+DI → controller+DTOs+mapper+validators → migration.
