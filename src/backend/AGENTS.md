# Backend Conventions (.NET 10 / C# 13)

> Follow the **Agent Workflow** in the root [`AGENTS.md`](../../AGENTS.md) — commit atomically, run `dotnet build` before each commit, and write session docs when asked.
>
> For explanations, rationale, and design decisions, see [`docs/backend-conventions.md`](../../docs/backend-conventions.md), [`docs/api-contract.md`](../../docs/api-contract.md), and [`docs/security.md`](../../docs/security.md).

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
│   │       ├── Dtos/
│   │       │   ├── {Operation}Input.cs
│   │       │   └── {Entity}Output.cs
│   │       └── Persistence/           # Optional — only if custom queries needed
│   │           └── I{Feature}Repository.cs
│   ├── Persistence/
│   │   └── IBaseEntityRepository.cs
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
│   │       ├── Persistence/       # Custom repository implementations (optional)
│   │       ├── Models/            # EF/Identity models
│   │       ├── Configurations/    # IEntityTypeConfiguration
│   │       ├── Extensions/        # DI registration
│   │       ├── Options/           # Configuration binding classes
│   │       └── Constants/         # Feature-specific constants
│   ├── Persistence/
│   │   ├── MyProjectDbContext.cs
│   │   ├── BaseEntityRepository.cs
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

### Collection Return Types — Narrowest Type That Fits

| Type | When | Why |
|---|---|---|
| `IReadOnlyList<T>` | Default for returning collections | Materialized, indexed, signals immutability |
| `IReadOnlyCollection<T>` | Need count but not index access | Rare — `IReadOnlyList<T>` is almost always better |
| `IEnumerable<T>` | Lazy/streaming evaluation is genuinely needed | Almost never in this codebase — repositories materialize everything |
| `List<T>` | Internal working variable only | Never as a return type on public/internal interfaces — don't expose mutability |
| `T[]` | Performance-critical internals (`Span<T>`, interop) | Never for public API contracts — mutable and non-resizable, `IReadOnlyList<T>` is strictly better |

### XML Documentation

All **public and internal API surface** must have `/// <summary>` XML docs. This includes interfaces, extension method classes, middleware, shared base classes, and service implementations — not just controllers and DTOs.

| Item | What to document |
|---|---|
| **Interfaces** (`I{Feature}Service`) | Class-level summary of the contract; each method's purpose, parameters, and return semantics |
| **Extension classes** (`CorsExtensions`, `SecurityHeaderExtensions`) | Class-level summary of what the extensions configure; method-level docs explaining behavior, parameters, and side effects |
| **Middleware** (`ExceptionHandlingMiddleware`) | Class-level summary; document which exceptions map to which status codes |
| **Shared base classes** (`ApiController`, `BaseEntityConfiguration<T>`) | Class-level summary of what inheritors get for free |
| **Options classes** | Already covered in the [Options Pattern](#options-pattern) section — every class and property gets `/// <summary>` |
| **Controllers and DTOs** | Already covered in the [OpenAPI](#openapi-specification--the-api-contract) section — `/// <summary>` on actions and every property |

## Entity Definition

All domain entities extend `BaseEntity`, which provides audit fields and soft delete. The `AuditingInterceptor` automatically populates audit timestamps — never set these manually.

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

Use `Result` / `Result<T>` for expected business failures. Never throw exceptions for business logic.

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
- Use `IOptions<T>` for configuration
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
- Always include `/// <summary>` XML docs — these generate OpenAPI descriptions
- Always include `[ProducesResponseType]` for all possible status codes
- Always accept `CancellationToken` as the last parameter
- **Never add `/// <param name="cancellationToken">`** — it leaks into `requestBody.description`
- Only add `/// <param>` tags for parameters visible in the OAS
- **Never return anonymous objects or raw strings** — always use defined DTOs
- **Never use `StatusCode(int, object)`** — loses type info. Use `Ok()`, `Created(string.Empty, response)`, `BadRequest(error)`, etc.
- **For 201 Created**, use `Created(string.Empty, response)` — not `CreatedAtAction` or `StatusCode(201, response)`
- **Never use `#pragma warning disable`** for XML doc warnings
- Use primary constructors for dependency injection
- Map requests to inputs via mapper extension methods

## Validation

FluentValidation validators are auto-discovered from the WebApi assembly. Co-locate validators with their request DTOs:

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

Simple DTOs can also use data annotations — both validation systems work together.

## Error Handling

| Scenario | Action |
|---|---|
| Expected business failure | Return `Result.Failure("message")` from service |
| Not found | Throw `KeyNotFoundException` → middleware returns 404 |
| Pagination error | Throw `PaginationException` → middleware returns 400 |
| Unexpected error | Let propagate → middleware returns 500 with `ErrorResponse` |

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

`ErrorResponse` is the **only** error body type across the entire API. Never return raw strings, anonymous objects, or other shapes for errors.

## Security

> For security architecture, cookie design, CSP rationale, and header explanations, see [`docs/security.md`](../../docs/security.md).

### Principle: Restrictive by Default

Always default to the most restrictive security posture and only relax constraints when explicitly required.

### Security Response Headers

`SecurityHeaderExtensions.UseSecurityHeaders()` adds security headers to every API response:

| Header | Value | Purpose |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing (XSS via content type confusion) |
| `X-Frame-Options` | `DENY` | Prevents embedding in iframes (clickjacking) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Prevents leaking URL paths to third-party sites |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Disables browser APIs the app doesn't use |

`Permissions-Policy` uses `()` (empty allowlist) to deny. To enable a browser API, change the specific directive to `(self)` — never remove the header or use `*`.

HSTS is enabled via `app.UseHsts()` in non-development environments.

## Repository Pattern & Persistence

> For design rationale (why repositories return materialized objects, save boundaries, transaction strategy), see [`docs/backend-conventions.md`](../../docs/backend-conventions.md#persistence).

### DbContext Lifecycle

`MyProjectDbContext` is scoped (one per HTTP request) via `AddDbContext`.

- **Services** that need direct query access inject `MyProjectDbContext`
- **Repositories** wrap `DbContext` with entity-specific query methods
- **Never** use `IDbContextFactory` for HTTP request handling

### Generic Repository — `IBaseEntityRepository<T>`

Standard CRUD with automatic soft-delete filtering:

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

All queries automatically exclude soft-deleted records via a global query filter in `BaseEntityConfiguration`. Use `.IgnoreQueryFilters()` when querying deleted entities.

The open generic `IBaseEntityRepository<T> → BaseEntityRepository<T>` covers entities that only need standard CRUD. For custom queries, create a feature-specific repository.

### Custom Repositories

When an entity needs queries beyond basic CRUD:

**1. Define the interface (Application layer):**

```csharp
// Application/Features/Orders/Persistence/IOrderRepository.cs
public interface IOrderRepository : IBaseEntityRepository<Order>
{
    /// <summary>
    /// Gets all orders for a specific user, ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an order by its order number. Returns null if not found.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
}
```

**2. Implement (Infrastructure layer):**

```csharp
// Infrastructure/Features/Orders/Persistence/OrderRepository.cs
internal class OrderRepository(MyProjectDbContext dbContext)
    : BaseEntityRepository<Order>(dbContext), IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken ct = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Paginate(pageNumber, pageSize)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }
}
```

**3. Register in DI (Infrastructure layer):**

```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

Key rules:
- **Return materialized objects, never `IQueryable`** — repositories call `ToListAsync`, `FirstOrDefaultAsync`, etc. before returning. If a service needs a different query, add a new repository method.
- **Interface** in `Application/Features/{Feature}/Persistence/` — extends `IBaseEntityRepository<T>`
- **Implementation** in `Infrastructure/Features/{Feature}/Persistence/` — extends `BaseEntityRepository<T>`, marked `internal`
- **Override `virtual` methods** from `BaseEntityRepository<T>` for custom behavior (e.g., eager loading with `.Include()`)
- **Inject the specific interface** (`IOrderRepository`) in services, not the generic one

### Saving Changes

**Services** call `SaveChangesAsync`, not repositories. `SaveChangesAsync` wraps all pending changes in a single implicit transaction:

```csharp
await repository.AddAsync(entity, ct);
await dbContext.SaveChangesAsync(ct);
```

### Explicit Transactions

Use only when multiple `SaveChangesAsync` calls must be atomic:

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
try
{
    await dbContext.Orders.AddAsync(order, ct);
    await dbContext.SaveChangesAsync(ct); // order.Id is now set

    var audit = new AuditEntry(order.Id, "Created");
    await dbContext.AuditEntries.AddAsync(audit, ct);
    await dbContext.SaveChangesAsync(ct);

    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

| Pattern | When |
|---|---|
| `dbContext.SaveChangesAsync()` | Default — single batch of changes, implicitly transactional |
| `BeginTransactionAsync` / `CommitAsync` | Multiple `SaveChangesAsync` calls that must succeed or fail together |

### Optimistic Concurrency

Not enforced globally yet. When a use case emerges, discuss the strategy per-entity. Options: `[ConcurrencyCheck]`, `IsConcurrencyToken()`, or PostgreSQL's `xmin`.

## Pagination

Use the shared abstract base classes for paginated endpoints:

- `PaginatedRequest` — `PageNumber` (default 1, min 1) and `PageSize` (default 10, max 100)
- `PaginatedResponse<T>` — `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`

The `PaginationExtensions.Paginate()` extension method applies Skip/Take with validation and caps page size at 100.

## Caching

`ICacheService` wraps Redis with JSON serialization. Use the cache-aside pattern:

```csharp
// Cache-aside pattern
var user = await cacheService.GetOrSetAsync(
    CacheKeys.User(userId),
    async ct => await FetchUserFromDb(userId, ct),
    CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(1)),
    cancellationToken
);
```

Cache keys are defined in `Application/Caching/Constants/CacheKeys.cs` as static methods (e.g., `CacheKeys.User(userId)` → `"user:{guid}"`). The `UserCacheInvalidationInterceptor` automatically invalidates user cache entries on modification.

## Options Pattern

> For rationale (why `sealed`, why `ValidateOnStart`, why `[ValidateObjectMembers]`), see [`docs/backend-conventions.md`](../../docs/backend-conventions.md#options-pattern).

Configuration classes use the **Options pattern** with Data Annotations + `IValidatableObject` for validation, validated at startup via `ValidateDataAnnotations()` + `ValidateOnStart()`.

### Defining Options Classes

Options classes are `public sealed class` with `const string SectionName`. The class name corresponds to the closest parent `appsettings.json` section. Properties use `init`-only setters:

```csharp
// Infrastructure/Features/Authentication/Options/AuthenticationOptions.cs
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    [ValidateObjectMembers]           // Recurses into JwtOptions data annotations
    public JwtOptions Jwt { get; init; } = new();

    public sealed class JwtOptions
    {
        [Required]
        [MinLength(32)]
        public string Key { get; init; } = string.Empty;

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Range(1, 120)]
        public int ExpiresInMinutes { get; init; } = 10;

        [ValidateObjectMembers]       // Recurses into RefreshTokenOptions
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

Every Options class and property must have `/// <summary>` XML docs, including nested child classes:

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
    [ValidateObjectMembers]
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
- **Class-level** `/// <summary>` — describe what the options configure and which section they map to
- **Property-level** `/// <summary>` — start with "Gets or sets…", mention defaults and constraints
- **Nested class** `/// <summary>` — describe what the sub-section configures

### Child Options (Sub-Sections)

Child options model nested `appsettings.json` sections. **Always nest them as `public sealed class` inside the parent.** No `SectionName`, bound automatically through the parent, not registered independently.

```csharp
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Required]
    [ValidateObjectMembers]
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
| **`[ValidateObjectMembers]`** | Properties holding nested options objects — ensures `ValidateDataAnnotations()` recurses into children. **Required on every property that holds a child options object with data annotations.** |
| **`IValidatableObject.Validate()`** | Cross-property rules, conditional validation, business logic checks |

**`[ValidateObjectMembers]`** tells `ValidateDataAnnotations()` to recurse into a nested object. Without it, annotations on nested objects are silently ignored.

**Never** use `IValidateOptions<T>` — keep all validation on the options class itself.

When a parent needs **conditional** child validation (because `[ValidateObjectMembers]` validates unconditionally), delegate manually:

```csharp
// Parent delegates to children conditionally — CachingOptions pattern
// Use this ONLY when you need conditional validation (e.g., validate Redis only when enabled)
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    if (Redis.Enabled)
    {
        foreach (var result in Redis.Validate(validationContext))
            yield return result;
    }
}
```

For unconditional child validation, prefer `[ValidateObjectMembers]` over manual delegation.

### Registration

Register in the feature's DI extension:

```csharp
services.AddOptions<AuthenticationOptions>()
    .BindConfiguration(AuthenticationOptions.SectionName)
    .ValidateDataAnnotations()    // Runs data annotations AND IValidatableObject.Validate()
    .ValidateOnStart();           // Fail fast at startup
```

Only **root** options classes (with `SectionName`) are registered. Nested classes bind through the parent.

### Consuming Options

**Runtime** — `IOptions<T>` in services, extract `.Value` to a readonly field:

```csharp
internal class MyService(IOptions<AuthenticationOptions> authenticationOptions) : IMyService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;
}
```

**Startup** — when needed during DI registration (before the container is built), read from `IConfiguration`:

```csharp
var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
    ?? throw new InvalidOperationException("Authentication options are not configured properly.");
```

**Never** use `IOptionsMonitor<T>` or `IOptionsSnapshot<T>` — all configuration is static.

## C# 13 Extension Member Syntax

**Always** use the new extension member syntax for new extension methods:

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

> For rationale (why strings in API, integers in DB), see [`docs/backend-conventions.md`](../../docs/backend-conventions.md#enum-handling).

Enums are **strings in the API** (JSON, OAS, TypeScript) and **integers in the database** (EF Core default).

### Declaration

Define in the **Domain** layer:

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
- **Always assign explicit integer values** — inserting a member between existing ones would corrupt stored data
- **PascalCase** member names
- Place in `Domain/Entities/` or `Domain/Enums/` if shared

### Runtime JSON Serialization

`JsonStringEnumConverter` is registered globally in `Program.cs`. All API responses serialize enums as `"Shipped"` not `2`. Never remove this converter.

### OpenAPI Spec

`EnumSchemaTransformer` sets `type: string`, lists all members in `enum` array, and handles nullable enums (`type: [string, null]`).

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

With the above setup, `npm run api:generate` produces:

```typescript
// Non-nullable enum → union of literal strings
status: "Pending" | "Processing" | "Shipped" | "Delivered" | "Cancelled";

// Nullable enum → union of literal strings + undefined
status?: "Pending" | "Processing" | "Shipped" | "Delivered" | "Cancelled";
```

If you see `unknown` in generated types for an enum field, the transformer or annotations are misconfigured.

### EF Core Storage

Store as **integers** (default). Add `.HasComment()` to document values:

```csharp
builder.Property(e => e.Status)
    .HasComment("OrderStatus enum: 0=Pending, 1=Processing, 2=Shipped, 3=Delivered, 4=Cancelled");
```

Comment format: `EnumTypeName enum: N=Member, ...`. Do **not** use `HasConversion<string>()`.

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

> For pipeline overview, DTO design rationale, and frontend type usage, see [`docs/api-contract.md`](../../docs/api-contract.md).

The OAS at `/openapi/v1.json` is the single source of truth for the frontend. Treat spec output as a first-class deliverable.

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

These `<summary>` tags become property descriptions in the OAS schema.

### Nullability → Required/Optional in OAS

```csharp
public string Email { get; init; } = string.Empty;  // → required in OAS, non-nullable in TypeScript
public string? FirstName { get; init; }              // → optional in OAS, T | undefined in TypeScript
```

See the [Nullable Reference Types](#nullable-reference-types) section.

### Validation Annotations → OAS Constraints

Data annotations on DTOs flow into the spec:

```csharp
[MaxLength(255)]           // → maxLength: 255
[MinLength(6)]             // → minLength: 6
[Range(1, 100)]            // → minimum: 1, maximum: 100
[EmailAddress]             // → format: email
[Required]                 // → required (in addition to non-nullable)
```

Use alongside FluentValidation — annotations feed the spec, FluentValidation handles complex rules.

### OAS Compliance Checklist

- [ ] `/// <summary>` on the controller action
- [ ] `/// <param>` for visible parameters — **never** for `CancellationToken`
- [ ] `/// <response code="...">` for every possible status code
- [ ] `[ProducesResponseType]` for every status code — `typeof(T)` for response bodies, `typeof(ErrorResponse)` for error bodies
- [ ] `ActionResult<T>` return type (not bare `ActionResult`) when returning a success body
- [ ] Error responses always return `new ErrorResponse { Message = ... }`
- [ ] `/// <summary>` on every DTO class and property
- [ ] Correct nullability (`string` vs `string?`)
- [ ] Data annotations on request DTOs
- [ ] `[Description("...")]` on base-class query parameter properties
- [ ] `CancellationToken` as last parameter, passed through to services — no `<param>` XML doc
- [ ] Route uses lowercase
- [ ] Enums verified in Scalar (string type, all members listed)
- [ ] No `#pragma warning disable`
- [ ] After DTO changes: `npm run api:generate` from `src/frontend/` → commit `v1.d.ts`

## Adding a New Feature — Checklist

1. **Domain**: Create entity in `Domain/Entities/` extending `BaseEntity`
2. **Domain**: If the entity has enum properties, define them with explicit integer values in `Domain/Entities/` (or `Domain/Enums/` if shared)
3. **Application**: Define `I{Feature}Service` in `Application/Features/{Feature}/`
4. **Application**: Create Input/Output record DTOs in `Application/Features/{Feature}/Dtos/`
5. **Application**: If the entity needs custom queries, define `I{Feature}Repository` in `Application/Features/{Feature}/Persistence/` extending `IBaseEntityRepository<T>`
6. **Infrastructure**: Implement service in `Infrastructure/Features/{Feature}/Services/` (mark `internal`)
7. **Infrastructure**: If custom repository was defined, implement in `Infrastructure/Features/{Feature}/Persistence/` extending `BaseEntityRepository<T>` (mark `internal`)
8. **Infrastructure**: Add EF configuration in `Infrastructure/Features/{Feature}/Configurations/` (extend `BaseEntityConfiguration<T>`) — add `.HasComment()` on enum columns
9. **Infrastructure**: Create DI extension in `Infrastructure/Features/{Feature}/Extensions/ServiceCollectionExtensions.cs`
10. **Infrastructure**: Add `DbSet<Entity>` to `MyProjectDbContext`
11. **WebApi**: Create controller in `WebApi/Features/{Feature}/` (extend `ApiController` or `ControllerBase`)
12. **WebApi**: Create Request/Response DTOs in `WebApi/Features/{Feature}/Dtos/{Operation}/`
13. **WebApi**: Create Mapper in `WebApi/Features/{Feature}/{Feature}Mapper.cs`
14. **WebApi**: Add validators co-located with request DTOs
15. **WebApi**: Wire DI call in `Program.cs`
16. **Migration**: `dotnet ef migrations add ...`
17. **Docs**: Update `docs/` and AGENTS.md if the feature introduces new patterns or conventions (see root `AGENTS.md` — Documentation Maintenance)

Commit atomically: entity+config → service interface+DTOs+repository interface → service implementation+repository implementation+DI → controller+DTOs+mapper+validators → migration.
