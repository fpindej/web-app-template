---
paths:
  - "src/**"
---

# Testing Rules

Extends CLAUDE.md with test project structure and patterns.

## Backend Test Projects
- `Unit.Tests` - pure logic (Shared, Domain, Application), no mocks, no DI
- `Component.Tests` - service logic with `TestDbContextFactory` (InMemory), `NSubstitute`, `IdentityMockHelpers`
- `Api.Tests` - full HTTP pipeline with `CustomWebApplicationFactory`, `TestAuthHandler`
- `Architecture.Tests` - layer deps, naming, visibility via NetArchTest

## Backend API Test Auth
- Default: `"Authorization", "Test"` header (basic user)
- Specific permissions: `TestAuth.WithPermissions(...)`
- Superuser: `TestAuth.Superuser()`
- Response contracts: frozen records in `Contracts/ResponseContracts.cs`

## Frontend Testing
- Co-locate: `foo.ts` -> `foo.test.ts`
- Explicit imports: `import { describe, it, expect, vi } from 'vitest'`
- Default env is `node` - add `// @vitest-environment jsdom` for DOM tests
- `restoreMocks: true` handles cleanup globally - no manual restore needed

## General
- No conditional tests - always assert deterministic outcomes
- No `any` in test code - type mocks properly
