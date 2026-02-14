# Backend Conventions (.NET 10 / C# 13)

## Project Structure

```
src/backend/
├── MyProject.Domain/              # Entities, value objects, Result pattern
│   ├── Entities/
│   │   └── BaseEntity.cs
│   ├── PhoneNumberHelper.cs       # Phone normalization (source-generated regex)
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
    ├── Authorization/             # Permission-based authorization
    │   ├── RequirePermissionAttribute.cs
    │   ├── PermissionPolicyProvider.cs
    │   ├── PermissionAuthorizationHandler.cs
    │   └── PermissionRequirement.cs
    ├── Shared/                    # ApiController, ErrorResponse, PaginatedRequest/Response, ValidationConstants
    ├── Middlewares/                # ExceptionHandlingMiddleware
    ├── Extensions/                # CORS, rate limiting
    └── Options/                   # CorsOptions, RateLimitingOptions
```

## Dependency Management

### Centralized Package Versioning

NuGet package versions are managed centrally in `Directory.Packages.props`. **Never add version attributes to individual `.csproj` files.**

To add a new dependency:

1. Add the version to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
   ```
2. Reference it in the appropriate `.csproj` **without** a version:
   ```xml
   <PackageReference Include="Newtonsoft.Json" />
   ```

`Directory.Build.props` sets shared properties across all projects: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`.

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

### Time Abstraction — Always Use `TimeProvider`

Never use `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, or `DateTime.Now` directly. Always inject `TimeProvider` and call `timeProvider.GetUtcNow()`:

```csharp
// ✅ Correct — inject TimeProvider
internal class MyService(TimeProvider timeProvider) : IMyService
{
    public void DoSomething()
    {
        var now = timeProvider.GetUtcNow();
    }
}

// ❌ Wrong — direct static call, untestable
var now = DateTimeOffset.UtcNow;
var now = DateTime.UtcNow;
```

`TimeProvider.System` is registered as a singleton in `Program.cs`:

```csharp
builder.Services.AddSingleton(TimeProvider.System);
```

This enables deterministic time in tests by substituting a `FakeTimeProvider`. In DTOs and other types that cannot take constructor dependencies, compute time-dependent values in the service layer and pass them as parameters.

### Collection Return Types — Narrowest Type That Fits

| Type | When | Why |
|---|---|---|
| `IReadOnlyList<T>` | Default for returning collections | Materialized, indexed, signals immutability |
| `IReadOnlyCollection<T>` | Need count but not index access | Rare — `IReadOnlyList<T>` is almost always better |
| `IEnumerable<T>` | Lazy/streaming evaluation is genuinely needed | Almost never in this codebase — repositories materialize everything |
| `List<T>` | Internal working variable only | Never as a return type on public/internal interfaces — don't expose mutability |
| `T[]` | Performance-critical internals (`Span<T>`, interop) | Never for public API contracts — mutable and non-resizable, `IReadOnlyList<T>` is strictly better |

The same "minimal scope" principle from access modifiers applies: don't return `List<T>` when the caller shouldn't add/remove items, don't return `IEnumerable<T>` when the data is already materialized.

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

When adding or modifying any of these types, apply the boy-scout rule: leave the file better than you found it. If a class is missing docs, add them while you're there.

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
- **Boolean naming**: Use `Is*`/`Has*` prefix in C# per .NET convention (e.g. `IsUsed`, `IsActive`, `HasExpired`), but map to prefix-free DB column names via `HasColumnName` (e.g. `Used`, `Active`, `Expired`) to keep the schema clean

### Soft-Delete & Restore

`BaseEntity` provides `SoftDelete()` and `Restore()` instance methods. **Always call these methods** — never set `IsDeleted` directly:

```csharp
entity.SoftDelete();  // Sets IsDeleted = true (idempotent — no-ops if already deleted)
entity.Restore();     // Sets IsDeleted = false, clears DeletedAt/DeletedBy (idempotent)
```

The `AuditingInterceptor` detects these state changes and automatically populates `DeletedAt`/`DeletedBy` on soft-delete and clears them on restore.

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

### Database Schema Strategy

Most entities use the default `public` schema. Feature-specific tables may use named schemas for logical grouping (e.g., `builder.ToTable("RefreshTokens", "auth")`). When adding a new entity, use the default schema unless the entity belongs to an existing named schema.

### Database Seeding

On startup, `InitializeDatabaseAsync` runs a four-part initialization:

1. **Migrations** (Development only) — auto-applies pending EF Core migrations
2. **Role seeding** (always) — ensures all roles from `AppRoles.All` exist via `RoleManager`
3. **Permission seeding** (always) — ensures default permissions are assigned to roles via `SeedRolePermissionsAsync()` (idempotent — checks existing claims before adding)
4. **Test user seeding** (Development only) — creates test users from `SeedUsers` constants

Roles are defined as `public const string` fields in `Application/Identity/Constants/AppRoles.cs`. Adding a new field is sufficient — `AppRoles.All` discovers roles automatically via reflection.

Permissions are seeded via `SeedRolePermissionsAsync()` in `ApplicationBuilderExtensions.cs`. It adds permission claims (claim type `"permission"`) to roles via `RoleManager.AddClaimAsync()`. Currently seeds `users.view`, `users.manage`, `users.assign_roles`, and `roles.view` for the Admin role.

Test user credentials are in `Infrastructure/Features/Authentication/Constants/SeedUsers.cs`. These are development-only — never used in production.

After creating entity + configuration:
1. Add `DbSet<Order>` to `MyProjectDbContext`
2. Run migration:

```bash
dotnet ef migrations add AddOrder \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --context MyProjectDbContext \
  --output-dir Features/Postgres/Migrations
```

## Result Pattern

Use `Result` / `Result<T>` for all operations that can fail expectedly. Never throw exceptions for business logic failures.

```csharp
// Success
return Result<Guid>.Success(entity.Id);
return Result.Success();

// Failure — use ErrorMessages constants for static messages
return Result<Guid>.Failure(ErrorMessages.Auth.LoginInvalidCredentials);
return Result.Failure(ErrorMessages.User.NotFound);

// Failure — use inline interpolation for dynamic messages
return Result.Failure($"Role '{roleName}' does not exist.");
```

Every `Result.Failure()` call **must** use an `ErrorMessages.*` constant when the message is static. For messages that include runtime values (usernames, role names, etc.), use inline string interpolation directly in the service.

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
        {
            var error = string.Join(" ", identityResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(error);
        }

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
- **`[ProducesResponseType]` goes on the action, never on the controller or base class.** Each action explicitly declares its complete response contract so the OAS entry is self-contained and precise. Even when multiple actions share a status code (e.g., 401 on all authorized endpoints, 429 on all rate-limited endpoints), repeat the attribute per-action — class-level placement silently applies to actions that don't need it, creating noise in the spec and misleading generated types. Example: only add 429 to endpoints with `[EnableRateLimiting]`, not to the base class just because the global limiter exists.
- Always accept `CancellationToken` as the last parameter on async endpoints
- **Never add `/// <param name="cancellationToken">`** — ASP.NET excludes `CancellationToken` from OAS parameters, but the `<param>` text leaks into `requestBody.description`. CS1573 is suppressed project-wide.
- Only add `/// <param>` tags for parameters that should appear in the OAS (request body, route/query params)
- **Never return anonymous objects or raw strings** from controllers — always use a defined DTO (`ErrorResponse` for errors, typed response DTOs for success). Anonymous objects produce untyped schemas in the OAS.
- **Never use `StatusCode(int, object)`** for responses with a body — the `object` parameter loses type information and the OAS generator cannot introspect the response schema. Use typed helpers instead: `Ok(response)`, `Created(string.Empty, response)`, `BadRequest(error)`, etc.
- **For 201 Created responses**, use `Created(string.Empty, response)` — not `CreatedAtAction` (which generates `Location` headers for MVC/Razor patterns this API doesn't use) and not `StatusCode(201, response)` (which loses type info).
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255)
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(ValidationConstants.PhoneNumberPattern)
            .WithMessage("Phone number must be a valid format (e.g. +420123456789)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
```

### Validation Rules

| Rule Type | Convention |
|---|---|
| **New passwords** | Mirror Identity policy: `MinimumLength(6)` + `Matches("[a-z]")` + `Matches("[A-Z]")` + `Matches("[0-9]")`. Identity requires digit, lowercase, uppercase but **not** non-alphanumeric. |
| **Existing passwords** (login, delete account) | `NotEmpty()` + `MaximumLength(255)` only. Never enforce policy on passwords the user already created — policy may have changed since. |
| **Optional fields** | Use `.When(x => !string.IsNullOrEmpty(x.Field))` to skip rules when the field is absent. |
| **URL fields** | Validate with `Uri.TryCreate` **and** restrict to `Uri.UriSchemeHttp` / `Uri.UriSchemeHttps` — reject `file://`, `ftp://`, etc. |

### Shared Validation Constants

When the same pattern (e.g., phone number regex) appears in multiple validators, extract it to `WebApi/Shared/ValidationConstants.cs` instead of duplicating a `private const` in each validator:

```csharp
// WebApi/Shared/ValidationConstants.cs
public static class ValidationConstants
{
    public const string PhoneNumberPattern = @"^(\+\d{1,3})? ?\d{6,14}$";
}
```

Simple DTOs can also use data annotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`, etc.) — both validation systems work together. Data annotations feed the OpenAPI spec; FluentValidation handles complex runtime rules.

## Error Handling

Three-tier strategy:

1. **Expected business failures** → Return `Result.Failure(ErrorMessages.X.Y)` from services
2. **Not found** → Throw `KeyNotFoundException` → `ExceptionHandlingMiddleware` returns 404
3. **Pagination errors** → Throw `PaginationException` → middleware returns 400
4. **Unexpected errors** → Let them propagate → middleware returns 500 with `ErrorResponse`

```csharp
// ExceptionHandlingMiddleware catches and maps exceptions:
// KeyNotFoundException     → 404 (logged as Warning)
// PaginationException      → 400 (logged as Warning)
// Everything else          → 500 (logged as Error, message: ErrorMessages.Server.InternalError)
```

The `ErrorResponse` shape:

```csharp
public class ErrorResponse
{
    public string? Message { get; init; }
    public string? Details { get; init; }    // Stack trace — Development only
}
```

`ErrorResponse` is the **only** error body type across the entire API — both controllers and middleware return it. The middleware serializes with explicit `JsonNamingPolicy.CamelCase` to match ASP.NET's controller serialization. Never return raw strings, anonymous objects, or other shapes for errors.

## Error Messages

Every failure in the system carries a descriptive, user-facing English message. Messages are organized as constants in `ErrorMessages.cs` (Domain layer) for static strings, or constructed inline for dynamic messages.

### `ErrorMessages` Static Class (Domain)

Error messages are defined as `const string` fields in `ErrorMessages.cs`, organized into nested static classes by domain area:

```csharp
public static class ErrorMessages
{
    public static class Auth
    {
        public const string LoginInvalidCredentials = "Invalid username or password.";
        public const string TokenExpired = "Refresh token has expired.";
        // ...
    }

    public static class User { /* ... */ }
    public static class Admin { /* ... */ }
    public static class Pagination { /* ... */ }
    public static class Server { /* ... */ }
    public static class Entity { /* ... */ }
}
```

### Rules

- **Use `ErrorMessages.*` constants for static messages** — every `Result.Failure()` call with a fixed string must reference a constant, not a string literal.
- **Use inline string interpolation for dynamic messages** — when the message includes runtime values (usernames, role names, entity IDs), construct the string in the service: `$"Role '{roleName}' does not exist."`
- **Messages are user-facing** — write clear, specific English text. The message flows directly to the frontend and is displayed to the user.
- **Add new constants to the appropriate nested class** — if no class fits, create a new one following the existing pattern.

### Identity Errors

ASP.NET Identity returns its own descriptive `.Description` strings (e.g., `"Username 'user@example.com' is already taken."`). These are already user-friendly — pass them through directly instead of mapping to custom messages:

```csharp
var identityResult = await userManager.CreateAsync(user, input.Password);
if (!identityResult.Succeeded)
{
    var error = string.Join(" ", identityResult.Errors.Select(e => e.Description));
    return Result<Guid>.Failure(error);
}
```

## Security

### Principle: Restrictive by Default

Always default to the most restrictive security posture and only relax constraints when a feature explicitly requires it. This applies to headers, permissions, CORS, cookie policies, and any browser-facing configuration.

### Security Response Headers

`SecurityHeaderExtensions.UseSecurityHeaders()` adds security headers to every API response. These are browser-instructional — they tell browsers how to behave when handling responses. They have **zero impact** on non-browser clients (mobile apps, curl, etc.).

| Header | Value | Purpose |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing (XSS via content type confusion) |
| `X-Frame-Options` | `DENY` | Prevents embedding in iframes (clickjacking) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Prevents leaking URL paths to third-party sites |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Disables browser APIs the app doesn't use |

`Permissions-Policy` uses `()` (empty allowlist) to deny access entirely. If a feature needs a browser API (e.g., webcam for avatar capture), change the value to `(self)` for that specific directive — never remove the header or use `*`.

HSTS (`Strict-Transport-Security`) is enabled via `app.UseHsts()` in non-development environments.

The frontend applies the same headers to page responses via the `handle` hook in `hooks.server.ts`. API proxy routes (`/api/*`) are skipped — they receive headers from the backend directly.

### ForwardedHeaders

The middleware pipeline configures `X-Forwarded-For` and `X-Forwarded-Proto` header processing for reverse proxy scenarios (Docker, nginx, load balancers). In production, `Request.Scheme` is manually overridden to `https`. Without this, rate limiting, logging, and CORS checks use the proxy's IP/scheme instead of the client's.

## Authorization — Roles & Permissions

### Role Hierarchy

Roles follow a strict hierarchy: `SuperAdmin` (rank 3) > `Admin` (rank 2) > `User` (rank 1). Custom roles have rank 0 (no hierarchy authority — they are permission bundles only).

`AppRoles.GetRoleRank()` and `AppRoles.GetHighestRank()` resolve numeric ranks for comparison. Authorization rules enforced by the Admin service:

| Rule | What it prevents |
|---|---|
| Hierarchy check | Cannot manage users whose highest role rank ≥ your own |
| Role assignment | Cannot assign roles at or above your own rank |
| Role removal | Cannot remove roles at or above your own rank |
| Self-protection | Cannot remove a role from yourself |
| Self-lock | Cannot lock your own account |
| Self-delete | Cannot delete your own account |

These rules prevent privilege escalation. The frontend mirrors this logic in `$lib/utils/roles.ts`.

### Permission System

Authorization uses atomic permissions checked via `[RequirePermission("permission.name")]` on controller actions. Permissions are stored as ASP.NET Identity role claims in the `AspNetRoleClaims` table and embedded in JWT tokens as `"permission"` claims.

#### Key Files

| File | Purpose |
|---|---|
| `Application/Identity/Constants/AppPermissions.cs` | Permission constants, `All` collection (reflection-discovered), `ByCategory` grouped dictionary |
| `Application/Identity/Constants/PermissionDefinition.cs` | Record: `(string Value, string Category)` |
| `WebApi/Authorization/RequirePermissionAttribute.cs` | `[RequirePermission("users.view")]` — sets policy to `Permission:users.view` |
| `WebApi/Authorization/PermissionPolicyProvider.cs` | Dynamic `IAuthorizationPolicyProvider` for `Permission:*` policies |
| `WebApi/Authorization/PermissionAuthorizationHandler.cs` | Checks SuperAdmin bypass → permission claim match → deny |
| `WebApi/Authorization/PermissionRequirement.cs` | `IAuthorizationRequirement` holding permission string |
| `Infrastructure/Features/Admin/Services/RoleManagementService.cs` | Role CRUD, permission assignment, security stamp rotation |

#### Permission Constants

Defined in `AppPermissions.cs` using nested static classes (mirrors `AppRoles` pattern):

| Permission | Description |
|---|---|
| `users.view` | View user list/details in admin panel |
| `users.manage` | Lock/unlock/delete users |
| `users.assign_roles` | Assign/remove roles to/from users |
| `roles.view` | View role list/details |
| `roles.manage` | Create/edit/delete custom roles and assign permissions |

`AppPermissions.All` discovers permissions via reflection (scans nested types for `const string` fields). `AppPermissions.ByCategory` groups them by category (class name).

#### Default Seeded Permissions

| Permission | SuperAdmin | Admin | User |
|---|---|---|---|
| `users.view` | implicit | seeded | — |
| `users.manage` | implicit | seeded | — |
| `users.assign_roles` | implicit | seeded | — |
| `roles.view` | implicit | seeded | — |
| `roles.manage` | implicit | — | — |

SuperAdmin has all permissions implicitly (code check in `PermissionAuthorizationHandler`, never stored in DB). Seeding is handled by `SeedRolePermissionsAsync()` in `ApplicationBuilderExtensions.cs` (idempotent — checks existing claims before adding).

#### Authorization Flow

```
Request with JWT
    │
    ▼
[RequirePermission("users.view")]
    │
    ▼
PermissionPolicyProvider           ← Resolves "Permission:users.view" policy dynamically
    │
    ▼
PermissionAuthorizationHandler
    ├── User is SuperAdmin? → Allow (implicit all)
    ├── User has "permission" claim matching? → Allow
    └── Otherwise → 403 Forbidden
```

#### JWT Permission Claims

`JwtTokenProvider` collects permission claims from all user roles via a single join query on `RoleClaims` + `Roles`, deduplicates them, and adds `new Claim("permission", value)` to the JWT. `UserService` returns `AppPermissions.All` values for SuperAdmin users in the `/api/users/me` response.

#### System Role Protection

| Role | Delete | Rename | Edit Permissions |
|---|---|---|---|
| SuperAdmin | no | no | no (implicit all) |
| Admin | no | no | yes |
| User | no | no | yes |
| Custom roles | yes (if 0 users) | yes | yes |

System role detection uses `AppRoles.All.Contains(name)` — no DB flag needed.

#### Permission Change Propagation

When permissions are changed on a role via `SetRolePermissionsAsync`, all users in that role have their:
1. Refresh tokens invalidated (deleted from DB)
2. Security stamps rotated (`UserManager.UpdateSecurityStampAsync`)
3. User cache entries cleared

This forces re-authentication, ensuring updated permissions take effect on next login.

#### Using `[RequirePermission]` on Endpoints

```csharp
// Single permission required
[HttpGet]
[RequirePermission(AppPermissions.Users.View)]
public async Task<ActionResult<IEnumerable<AdminUserResponse>>> ListUsers(...)

// Different permissions on different actions in the same controller
[HttpDelete("{id}")]
[RequirePermission(AppPermissions.Users.Manage)]
public async Task<ActionResult> DeleteUser(Guid id, ...)
```

Never use class-level `[Authorize(Roles = "...")]` on controllers that use permissions — apply `[RequirePermission]` per-action instead.

## Repository Pattern & Persistence

### DbContext Lifecycle

`MyProjectDbContext` is registered as **scoped** (one per HTTP request) via `AddDbContext`. This is the correct lifetime for web APIs — each request gets a clean change tracker.

- **Services** that need direct query access inject `MyProjectDbContext` via primary constructor
- **Repositories** wrap `DbContext` with entity-specific query methods
- **Never** use `IDbContextFactory` for HTTP request handling — it's for background services that need parallel/concurrent DB operations

### Generic Repository — `IBaseEntityRepository<T>`

Provides standard CRUD with automatic soft-delete filtering:

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

All queries automatically exclude soft-deleted records via a global EF Core query filter (`HasQueryFilter(e => !e.IsDeleted)`) configured in `BaseEntityConfiguration`. Use `.IgnoreQueryFilters()` when you need to query deleted entities (e.g., `RestoreAsync`).

The open generic registration `IBaseEntityRepository<T> → BaseEntityRepository<T>` covers entities that only need standard CRUD. For entities with custom queries, create a feature-specific repository (see below).

### Custom Repositories

When an entity needs queries beyond basic CRUD, create a dedicated repository:

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

The open generic registration still serves entities without custom repositories. Feature-specific registrations take precedence when the specific interface is injected.

Key rules:
- **Return materialized objects, never `IQueryable`** — all repository methods call `ToListAsync`, `FirstOrDefaultAsync`, etc. before returning. Repositories are the query boundary; services don't compose additional LINQ on top. If a service needs a different query, add a new method to the repository.
- **Interface** in `Application/Features/{Feature}/Persistence/` — extends `IBaseEntityRepository<T>`
- **Implementation** in `Infrastructure/Features/{Feature}/Persistence/` — extends `BaseEntityRepository<T>`, marked `internal`
- **Query methods** belong on the repository, not scattered across services — the repository is the single source of truth for how an entity is queried
- **Override `virtual` methods** from `BaseEntityRepository<T>` when you need custom behavior (e.g., eager loading with `.Include()`)
- **Inject the specific interface** (`IOrderRepository`) in services, not the generic one — this gives access to custom methods while inheriting all base CRUD operations

### Saving Changes

Repositories stage changes — they don't save them. **Services** are responsible for calling `SaveChangesAsync` on the `DbContext`. This keeps the save boundary explicit and lets a service coordinate multiple repository calls into a single atomic save.

`SaveChangesAsync` wraps all pending changes in a **single implicit transaction** — if any change fails, they all roll back. For most operations, this is sufficient:

```csharp
await repository.AddAsync(entity, ct);
await dbContext.SaveChangesAsync(ct);
```

### Explicit Transactions

Use explicit transactions only when you need **multiple `SaveChangesAsync` calls** to be atomic — for example, when you need an ID from the first save to use in the second:

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

When **not** to use explicit transactions:
- Single `SaveChangesAsync` call — already atomic
- Read-only queries — no writes to coordinate
- Operations across different services — redesign to keep the transaction boundary within one service method

| Pattern | When |
|---|---|
| `dbContext.SaveChangesAsync()` | Default — single batch of changes, implicitly transactional |
| `BeginTransactionAsync` / `CommitAsync` | Multiple `SaveChangesAsync` calls that must succeed or fail together |

### Optimistic Concurrency

Not enforced globally yet — no entities currently require it. When a use case emerges (e.g., concurrent writes to inventory, order status), discuss the strategy with the user and add concurrency tokens to that specific entity. Options include EF Core's `[ConcurrencyCheck]` attribute, `IsConcurrencyToken()` in Fluent API, or PostgreSQL's `xmin` system column. Handle `DbUpdateConcurrencyException` at the service or middleware level when introduced.

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
| **`[ValidateObjectMembers]`** | Properties holding nested options objects with their own data annotations — ensures `ValidateDataAnnotations()` recurses into children |
| **`IValidatableObject.Validate()`** | Cross-property rules, conditional validation, business logic checks |

**`[ValidateObjectMembers]`** (from `Microsoft.Extensions.Options`) tells `ValidateDataAnnotations()` to recurse into a nested object and validate its data annotations. Without it, `ValidateDataAnnotations()` only checks the root class — annotations on nested objects are silently ignored. **Every property that holds a child options object with data annotations must have `[ValidateObjectMembers]`.**

**Never** use `IValidateOptions<T>` — keep all validation on the options class itself via data annotations, `[ValidateObjectMembers]`, or `IValidatableObject`. This keeps validation co-located with the configuration it validates.

When a parent options class composes child options that have their own `IValidatableObject.Validate()` and validation must be **conditional**, the parent must delegate validation to the children manually (because `[ValidateObjectMembers]` validates unconditionally):

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

For the common case where child validation is unconditional, prefer `[ValidateObjectMembers]` over manual delegation — it's simpler and less error-prone.

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
- [ ] All `[ProducesResponseType]` attributes are on the action, not on the controller or base class — each action declares only the status codes it can actually produce (e.g., 429 only with `[EnableRateLimiting]`, 404 only on lookup endpoints)
- [ ] Route uses lowercase (`[Route("api/[controller]")]` + `LowercaseUrls = true`)
- [ ] Enums serialize as strings with all members listed (handled by `JsonStringEnumConverter` + `EnumSchemaTransformer` — verify in Scalar)
- [ ] No `#pragma warning disable` — CS1573 (partial param docs) is suppressed project-wide because omitting `CancellationToken` param tags is intentional
- [ ] After any response DTO change (new DTO, renamed/added/removed properties, changed nullability), regenerate frontend types: `npm run api:generate` from `src/frontend/` with the backend running — then commit `v1.d.ts`

## Testing

No test infrastructure is currently set up — no unit test or integration test projects exist in the solution. When tests are added, this section will document the testing frameworks, patterns, and conventions.

## Adding a New Feature

For step-by-step procedures (add entity, add endpoint, add feature), see [`SKILLS.md`](../../SKILLS.md). This file documents **conventions and patterns** — SKILLS.md documents **workflows and checklists**.
