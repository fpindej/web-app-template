# Agent Guidelines for MyProject Web API

This document provides AI agents with context about the codebase structure, conventions, and best practices.

## Quick Reference

| Aspect | Details |
|--------|---------|
| **Framework** | .NET 10 / C# 13 |
| **Architecture** | Clean Architecture (4 layers) |
| **Database** | PostgreSQL + EF Core |
| **Auth** | JWT in HttpOnly cookies |
| **Validation** | FluentValidation |
| **Logging** | Serilog |
| **Docs** | Scalar (OpenAPI) |

## Project Structure

```
src/
├── MyProject.Domain/           # Core domain entities, value objects
│   ├── Entities/               # Base entities with soft delete
│   └── Result.cs               # Result pattern implementation
│
├── MyProject.Application/      # Application contracts
│   ├── Features/               # Feature-based organization
│   │   └── {Feature}/
│   │       ├── I{Service}.cs   # Service interface
│   │       └── Dtos/           # Input/Output DTOs
│   └── Persistence/            # Repository interfaces
│
├── MyProject.Infrastructure/   # Implementation layer
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── Services/       # Service implementations
│   │       ├── Models/         # EF entities (if different from domain)
│   │       ├── Configurations/ # EF type configurations
│   │       ├── Extensions/     # DI registration
│   │       └── Options/        # Configuration options
│   ├── Persistence/
│   │   ├── MyProjectDbContext.cs
│   │   ├── Extensions/         # EF helpers, query extensions
│   │   └── Configurations/     # Shared EF configurations
│   └── Logging/                # Serilog configuration
│
└── MyProject.WebApi/           # API entry point
    ├── Features/
    │   └── {Feature}/
    │       ├── {Feature}Controller.cs
    │       ├── {Feature}Mapper.cs    # Optional mapping
    │       └── Dtos/
    │           └── {Operation}/      # Request/Response per operation
    ├── Shared/                 # Base classes, common DTOs
    ├── Middlewares/            # Custom middleware
    ├── Extensions/             # App configuration extensions
    └── Program.cs              # Application entry point
```

## Key Conventions

### 1. Result Pattern

Always use `Result` or `Result<T>` for operations that can fail expectedly:

```csharp
// In Application layer interface
Task<Result<Guid>> CreateAsync(CreateInput input);

// In Infrastructure implementation
public async Task<Result<Guid>> CreateAsync(CreateInput input)
{
    if (await ExistsAsync(input.Email))
        return Result<Guid>.Failure("Email already exists.");
    
    // ... create entity
    return Result<Guid>.Success(entity.Id);
}

// In Controller
var result = await service.CreateAsync(input);
if (!result.IsSuccess)
    return BadRequest(result.Error);
return CreatedAtAction(...);
```

### 2. Extension Method Syntax (C# 13)

This project uses the new C# 13 extension member syntax:

```csharp
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyFeature(IConfiguration config)
        {
            services.AddScoped<IMyService, MyService>();
            return services;
        }
    }
}
```

### 3. Primary Constructors

Use primary constructors for dependency injection:

```csharp
internal class MyService(
    IRepository repository,
    ILogger<MyService> logger,
    IOptions<MyOptions> options) : IMyService
{
    private readonly MyOptions _options = options.Value;
    
    public async Task DoWorkAsync() { ... }
}
```

### 4. Entity Configuration

Entities extend `BaseEntity` and use Fluent API configuration:

```csharp
// Domain entity
public class MyEntity : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    
    protected MyEntity() { } // EF constructor
    
    public MyEntity(string name, DateTime createdAt) : base(createdAt)
    {
        Name = name;
    }
}

// Configuration
internal class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("my_entities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(255).IsRequired();
    }
}
```

### 5. DTO Naming

| Layer | Pattern | Example |
|-------|---------|---------|
| WebApi Request | `{Operation}Request` | `LoginRequest`, `CreateUserRequest` |
| WebApi Response | `{Operation}Response` | `MeResponse`, `UserListResponse` |
| Application Input | `{Operation}Input` | `RegisterInput`, `UpdateUserInput` |
| Application Output | `{Entity}Output` | `UserOutput`, `OrderOutput` |

### 6. Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController(IMyService service) : ControllerBase
{
    /// <summary>
    /// Creates a new resource.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request.ToInput());
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value });
    }
}
```

## Common Tasks

### Adding a New Feature

1. **Define the interface** (`Application/Features/NewFeature/INewFeatureService.cs`)
2. **Create DTOs** (`Application/Features/NewFeature/Dtos/`)
3. **Implement the service** (`Infrastructure/Features/NewFeature/Services/NewFeatureService.cs`)
4. **Register in DI** (add to or create `Extensions/ServiceCollectionExtensions.cs`)
5. **Create controller** (`WebApi/Features/NewFeature/NewFeatureController.cs`)
6. **Add request/response DTOs** (`WebApi/Features/NewFeature/Dtos/`)

### Adding a New Entity

1. **Create entity** in `Domain/Entities/` extending `BaseEntity`
2. **Add DbSet** to `MyProjectDbContext`
3. **Create configuration** in `Infrastructure/Features/{Feature}/Configurations/`
4. **Add migration**: `dotnet ef migrations add AddNewEntity ...`

### Adding Validation

Create a validator class in the same folder as the request:

```csharp
public class CreateRequestValidator : AbstractValidator<CreateRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

Validators are auto-discovered from the WebApi assembly.

## Authentication Flow

The API uses cookie-based JWT authentication:

```
1. POST /api/auth/login
   Request: { username, password }
   Response: 200 OK (sets access_token and refresh_token cookies)

2. Any authenticated request
   Cookies are sent automatically
   JWT is extracted from access_token cookie

3. POST /api/auth/refresh
   Refresh token cookie is read automatically
   Response: 200 OK (new tokens set in cookies)

4. POST /api/auth/logout
   Clears cookies and revokes all user tokens
```

## Configuration Structure

```json
{
  "ConnectionStrings": {
    "Database": "Host=...;Database=...;Username=...;Password=..."
  },
  "Authentication": {
    "Jwt": {
      "Key": "secret-key",
      "Issuer": "issuer",
      "Audience": "audience",
      "ExpiresInMinutes": 10,
      "RefreshToken": { "ExpiresInDays": 7 }
    }
  },
  "Cors": {
    "AllowAllOrigins": false,
    "AllowedOrigins": ["https://example.com"],
    "PolicyName": "DefaultCorsPolicy"
  },
  "RateLimiting": {
    "Global": { "PermitLimit": 120, "Window": "00:01:00" }
  }
}
```

## Project Initialization

### Using the Init Scripts

The template includes scripts to rename the project and configure ports. **Run this first when starting a new project.**

**Windows (PowerShell):**
```powershell
# Interactive mode
.\init.ps1

# Non-interactive with parameters
.\init.ps1 -NewName "MyAwesomeApi" -BasePort 14000
```

**macOS / Linux:**
```bash
chmod +x init.sh
./init.sh
```

### Init Script Workflow

```
┌─────────────────────────────────────────────────────────────┐
│  1. Enter Project Name (e.g., MyAwesomeApi)                 │
│  2. Enter Base Port (default: 13000)                        │
│     → API: BasePort + 2 (13002)                             │
│     → DB:  BasePort + 4 (13004)                             │
├─────────────────────────────────────────────────────────────┤
│  3. Updates docker-compose.local.yml ports                  │
│  4. Updates appsettings.Development.json (DB port)          │
│  5. Updates http-client.env.json (API URL)                  │
│  6. Renames all files/folders/namespaces                    │
├─────────────────────────────────────────────────────────────┤
│  7. (Optional) Git commit rename changes                    │
│  8. (Optional) Create fresh Initial migration               │
│     → Restores dotnet-ef tool                               │
│     → Builds project                                        │
│     → Creates migration                                     │
│  9. (Optional) Git commit migration                         │
└─────────────────────────────────────────────────────────────┘
```

### Port Configuration Reference

| Service | Formula | Default Port |
|---------|---------|--------------|
| Web API | `BasePort + 2` | 13002 |
| PostgreSQL | `BasePort + 4` | 13004 |

### Files Modified by Init Script

| File | Changes |
|------|---------|
| `docker-compose.local.yml` | Port mappings |
| `appsettings.Development.json` | Database connection port |
| `http-client.env.json` | API base URL |
| All `*.cs`, `*.csproj`, `*.sln` files | Namespace/project references |
| All directories containing `MyProject` | Renamed to new project name |

## Important Files

| File | Purpose |
|------|---------|
| `init.ps1` / `init.sh` | Project initialization and renaming scripts |
| `Program.cs` | Application startup and middleware pipeline |
| `MyProjectDbContext.cs` | EF Core DbContext with role seeding |
| `AuthenticationService.cs` | JWT/cookie auth implementation |
| `ExceptionHandlingMiddleware.cs` | Global error handling |
| `Result.cs` | Result pattern for error handling |
| `BaseEntity.cs` | Base entity with audit fields and soft delete |
| `docker-compose.local.yml` | Local development environment |

## Error Handling

- **Expected errors**: Return `Result.Failure("message")` from services
- **Not found**: Throw `KeyNotFoundException` → 404
- **Pagination errors**: Throw `PaginationException` → 400
- **Unexpected errors**: Let them propagate → 500 (middleware handles)

## Testing

Use the `auth-flow.http` file with the REST Client extension:

```http
### Login
POST {{baseUrl}}/api/auth/login
Content-Type: application/json

{
  "username": "test@test.com",
  "password": "Test123!"
}

### Get current user (requires cookies from login)
GET {{baseUrl}}/api/auth/me
```

Environment configuration in `http-client.env.json`.

