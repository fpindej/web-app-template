# Backend Conventions (.NET 10 / C# 13)

> Follow the **Agent Workflow** in the root [`AGENTS.md`](../../AGENTS.md) — commit atomically, run `dotnet build` before each commit, and write session docs at the end of each session.

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

    public void Complete() => Status = OrderStatus.Completed;
}
```

Key rules:
- **Private setters** on all properties — enforce invariants through methods
- **Protected parameterless constructor** for EF Core materialization
- **Public constructor** for domain creation with required parameters
- **Generate `Id`** in the constructor

## EF Core Configuration

Configurations inherit from `BaseEntityConfiguration<T>`, which handles all `BaseEntity` fields (primary key, audit columns, soft delete index). Override `ConfigureEntity` to add entity-specific mapping:

```csharp
// Infrastructure/Features/Orders/Configurations/OrderConfiguration.cs
internal class OrderConfiguration : BaseEntityConfiguration<Order>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>();
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
var result = await service.CreateAsync(input);
if (!result.IsSuccess)
    return BadRequest(result.Error);
return CreatedAtAction(nameof(Get), new { id = result.Value });
```

## Service Composition

### 1. Define Interface (Application Layer)

```csharp
// Application/Features/Authentication/IAuthenticationService.cs
public interface IAuthenticationService
{
    Task<Result> Login(string username, string password, CancellationToken cancellationToken = default);
    Task<Result<Guid>> Register(RegisterInput input);
    Task Logout();
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
    IOptions<JwtOptions> jwtOptions,
    MyProjectDbContext dbContext,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

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

Then call from `Program.cs`:

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
public static class UserMapper
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
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authenticationService.Login(request.Username, request.Password, ct);
        if (!result.IsSuccess)
            return Unauthorized(result.Error);
        return Ok();
    }
}
```

Rules:
- Always include `/// <summary>` XML docs — these generate OpenAPI descriptions consumed by the frontend
- Always include `[ProducesResponseType]` for all possible status codes
- Always accept `CancellationToken` on async endpoints
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

All queries automatically exclude soft-deleted records (`WHERE NOT is_deleted`). Use `IUnitOfWork` for explicit save and transaction control:

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

## OpenAPI Specification

The API serves its OpenAPI spec at `/openapi/v1.json` (development only). Scalar UI is available at `/scalar/v1`. The frontend generates TypeScript types from this spec — XML docs on controllers and `[ProducesResponseType]` attributes directly affect the quality of generated frontend types.

## Adding a New Feature — Checklist

1. **Domain**: Create entity in `Domain/Entities/` extending `BaseEntity`
2. **Application**: Define `I{Feature}Service` in `Application/Features/{Feature}/`
3. **Application**: Create Input/Output record DTOs in `Application/Features/{Feature}/Dtos/`
4. **Infrastructure**: Implement service in `Infrastructure/Features/{Feature}/Services/` (mark `internal`)
5. **Infrastructure**: Add EF configuration in `Infrastructure/Features/{Feature}/Configurations/` (extend `BaseEntityConfiguration<T>`)
6. **Infrastructure**: Create DI extension in `Infrastructure/Features/{Feature}/Extensions/ServiceCollectionExtensions.cs`
7. **Infrastructure**: Add `DbSet<Entity>` to `MyProjectDbContext`
8. **WebApi**: Create controller in `WebApi/Features/{Feature}/` (extend `ApiController` or `ControllerBase`)
9. **WebApi**: Create Request/Response DTOs in `WebApi/Features/{Feature}/Dtos/{Operation}/`
10. **WebApi**: Create Mapper in `WebApi/Features/{Feature}/{Feature}Mapper.cs`
11. **WebApi**: Add validators co-located with request DTOs
12. **WebApi**: Wire DI call in `Program.cs`
13. **Migration**: `dotnet ef migrations add ...`

Commit atomically: entity+config → service interface+DTOs → service implementation+DI → controller+DTOs+mapper+validators → migration.
