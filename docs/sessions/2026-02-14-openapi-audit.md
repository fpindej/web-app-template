# OpenAPI Specification Audit

**Date**: 2026-02-14
**Scope**: Comprehensive audit of the OpenAPI/OAS setup, fixing spec accuracy issues

## Summary

Audited the full OpenAPI specification setup — document transformers, operation transformers, schema transformers, controller annotations, and Scalar UI configuration. Identified 6 issues, fixed the 3 that meaningfully impact spec consumers, and evaluated the remaining 3 as not worth fixing.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `WebApi/Features/OpenApi/Transformers/BearerSecurityOperationTransformer.cs` | New operation transformer that applies `bearerAuth` security requirement to `[Authorize]` endpoints | Security scheme was defined in the spec but never applied to operations — clients couldn't see which endpoints require auth |
| `WebApi/Features/OpenApi/Extensions/WebApplicationBuilderExtensions.cs` | Registered `BearerSecurityOperationTransformer` | Wire up the new transformer |
| `WebApi/Features/Admin/AdminController.cs` | Added `typeof(ErrorResponse)` to all 28 untyped 401/403 `ProducesResponseType` attributes; added `[Tags("Admin")]` | Spec consumers see the error response schema; endpoints grouped under "Admin" tag |
| `WebApi/Features/Admin/JobsController.cs` | Added `typeof(ErrorResponse)` to all 14 untyped 401/403 `ProducesResponseType` attributes; added `[Tags("Jobs")]` | Same as above; grouped under "Jobs" tag |
| `WebApi/Features/Users/UsersController.cs` | Added `typeof(ErrorResponse)` to 3 untyped 401 `ProducesResponseType` attributes; added `[Tags("Users")]` | Same as above; grouped under "Users" tag |
| `WebApi/Features/Authentication/AuthController.cs` | Added `typeof(ErrorResponse)` to 2 untyped 401 `ProducesResponseType` attributes (logout, change-password); added `[Tags("Auth")]` | Same as above; grouped under "Auth" tag |

## Decisions & Reasoning

### Fix 1: Apply bearerAuth via operation transformer

- **Choice**: Created a dedicated `IOpenApiOperationTransformer` that inspects `[Authorize]` / `[AllowAnonymous]` metadata
- **Alternatives considered**: (a) Global security requirement on the document, (b) Per-endpoint `[OpenApiSecurity]` attributes
- **Reasoning**: Global security would mark anonymous endpoints (login, register, refresh) as requiring auth. Per-endpoint attributes would be verbose and error-prone. The transformer automatically detects authorization metadata, so new endpoints get correct security annotations without manual work.

### Fix 2: Typed ErrorResponse on 401/403

- **Choice**: Added `typeof(ErrorResponse)` to all `ProducesResponseType` attributes for 401 and 403 status codes
- **Alternatives considered**: Leaving them untyped
- **Reasoning**: Untyped error responses mean generated clients don't know the error shape. All controllers already return `ErrorResponse` instances at runtime, so the spec should reflect that.

### Fix 3: Explicit Tags on controllers

- **Choice**: Added `[Tags("Admin")]`, `[Tags("Jobs")]`, `[Tags("Users")]`, `[Tags("Auth")]` to each controller
- **Alternatives considered**: Relying on ASP.NET's auto-generated tags from controller names
- **Reasoning**: Explicit tags ensure stable, predictable grouping in Scalar UI regardless of controller naming changes. `JobsController` shares the `api/v1/admin` route prefix with `AdminController` — without explicit tags, both would auto-tag as "Admin", merging their operations.

### Items evaluated but not fixed

- **Rate limit values in spec (item 4)**: Adding concrete numbers (e.g., "10 requests per minute") to the OpenAPI spec would couple the spec to runtime configuration. The `429` response is already documented, and `Retry-After` headers tell clients when to retry. Not worth the maintenance burden.
- **`useCookies` query parameter description (item 5)**: Already handled by `CamelCaseQueryParameterTransformer` which propagates schema descriptions to parameters. Low priority.
- **`Set-Cookie` response headers (item 6)**: Documenting `Set-Cookie` headers in OpenAPI requires complex header schema definitions for marginal value — web clients handle cookies automatically and don't need to parse them from the spec. Not worth the complexity.

## Follow-Up Items

- [ ] Regenerate frontend API types (`npm run api:generate`) after merging to pick up the new `ErrorResponse` schemas on 401/403 responses
