---
paths:
  - "src/backend/**"
---

# Backend API Rules

Extends CLAUDE.md Hard Rules with implementation patterns.

## Error Handling
- `ProblemFactory.Create(result.Error, result.ErrorType)` for error responses - never `NotFound()`, `BadRequest()`, or anonymous objects
- Client-facing errors: `ErrorMessages.*` entries only (`Error` = stable snake_case `code` + message) - runtime values go in `ILogger`, never in `Result.Failure()`. Every ProblemDetails carries `code`; renaming a code is a breaking change

## Controllers
- Authenticated endpoints extend `ApiController` - public endpoints use `ControllerBase` directly
- Always: `/// <summary>`, `[ProducesResponseType]` per status code, `CancellationToken` as last param
- Never `/// <param name="cancellationToken">` - it leaks into OpenAPI `requestBody.description`
- `[ProducesResponseType]` without `typeof(...)` on error codes (400, 401, 403, 404, 429)

## Authorization
- `[RequirePermission("permission.name")]` on actions - never class-level `[Authorize(Roles)]` on permission controllers
- Role hierarchy enforced: cannot manage users at/above your rank

## Entities
- Extend `BaseEntity`, private setters, invariants via methods, protected parameterless ctor for EF
- Soft delete via `entity.SoftDelete()` / `entity.Restore()` - never set `IsDeleted` directly
