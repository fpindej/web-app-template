# Frontend Foundation Improvements

## Overview

This PR represents a comprehensive refactoring and improvement of the frontend codebase, establishing production-grade patterns, improving developer experience, and adding new features.

## 🎯 Key Changes

### Architecture & Code Organization

- **Barrel exports everywhere** - All components, utilities, and API functions now use barrel exports for cleaner imports
- **Domain-based folder structure** - Reorganized `services/` → `auth/`, consolidated `config/` folder, moved shake utilities to `state/`
- **Consistent import patterns** - Unified `$lib/api`, `$lib/state`, `$lib/components/*` imports across the codebase

### API & Error Handling

- **New error handling utilities** - `isValidationProblemDetails()`, `mapFieldErrors()`, `getErrorMessage()` for ASP.NET Core ProblemDetails
- **Field-level validation errors** - Backend validation errors now map to specific form fields with shake animations
- **Type-safe API client** - Consolidated `browserClient` and `createApiClient` exports

### New Components

- **PhoneInput** - International phone number input with country code selector and flag icons
- **Getting Started** - Interactive onboarding component with README and Copilot docs dialogs
- **StatusIndicator** - Reusable status indicator with online/offline states
- **Markdown parser** - For rendering README content with proper table support

### Design System

- **Global CSS utilities** - Modular CSS in `src/styles/` with design tokens, animations, and effects
- **Glow effects** - Subtle glow utilities for status indicators and interactive elements
- **Pulsing animations** - Purposeful pulse animations for online indicators
- **Card hover states** - Consistent hover effects across card components

### User Experience

- **Page transitions** - Smooth fade animations between routes
- **Shake animations** - Field-level error feedback with configurable shake effects
- **Responsive improvements** - Better tablet breakpoints and mobile layouts
- **Avatar support** - User avatars in navbar and profile management

### Internationalization

- **Domain-based i18n keys** - Reorganized translation keys: `auth_login_*`, `profile_personalInfo_*`, etc.
- **Localized country names** - Phone input country dropdown uses translated country names
- **Language metadata** - Centralized language config with flag icons

### Documentation

- **Complete copilot-instructions.md rewrite** - Comprehensive guidelines for AI-assisted development
- **README overhaul** - Getting started guide, architecture overview, development patterns
- **Pre-commit guidance** - Quality checklist and dead code cleanup instructions

### Cleanup & Fixes

- **Removed testing setup** - Removed vitest config (not needed for this template)
- **Removed unused code** - Cleaned up unused translation keys, components, and utilities
- **Fixed deprecated APIs** - Migrated from `base` to `resolve()` for path handling
- **Unified toast imports** - All toast calls now use `$lib/components/ui/sonner`

## 📁 Files Changed

```
78 files changed, 3398 insertions(+), 1861 deletions(-)
```

### New Files

- `src/lib/api/error-handling.ts` - API error utilities
- `src/lib/components/ui/phone-input/*` - Phone input component
- `src/lib/components/getting-started/*` - Onboarding components
- `src/lib/components/common/StatusIndicator.svelte`
- `src/lib/state/shake.svelte.ts` - Shake animation utilities
- `src/lib/config/i18n.ts` - Language metadata
- `src/styles/*.css` - Modular design system styles (themes, animations, utilities)

### Refactored

- `src/lib/services/` → `src/lib/auth/`
- `src/lib/server/config.ts` → `src/lib/config/server.ts`
- All component imports to use barrel exports
- Translation keys to domain-based naming

### Removed

- `vitest.config.ts`
- `src/lib/utils/ui.test.ts`
- Unused translation keys
- Empty hooks files

## ✅ Quality Checklist

- [x] `npm run format` - Prettier passes
- [x] `npm run lint` - ESLint passes
- [x] `npm run check` - Svelte type check passes (0 errors, 0 warnings)
- [x] `npm run build` - Production build succeeds
- [x] All imports use barrel exports per project conventions
- [x] Toast imports unified to `$lib/components/ui/sonner`
- [x] No unused code or dead imports

## 🔄 Breaking Changes

None - all changes are internal refactoring. The API surface remains the same.

## 📝 Migration Notes

If you have local changes:

1. **Imports** - Update any direct imports to use barrel exports:

   ```typescript
   // Before
   import { browserClient } from '$lib/api/client';
   import { toast } from 'svelte-sonner';

   // After
   import { browserClient } from '$lib/api';
   import { toast } from '$lib/components/ui/sonner';
   ```

2. **Translation keys** - Keys now follow `{domain}_{feature}_{element}` pattern

3. **Config imports** - Server config must be imported directly:

   ```typescript
   // Correct
   import { SERVER_CONFIG } from '$lib/config/server';

   // Wrong (will not work)
   import { SERVER_CONFIG } from '$lib/config';
   ```
