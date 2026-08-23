---
paths:
  - "src/frontend/**"
---

# Frontend Svelte Rules

Extends CLAUDE.md Hard Rules with implementation patterns.

## Svelte 5
- Reactive state in `.svelte.ts` files in `$lib/state/` only - never mix with pure `.ts` utils

## API Client
- File uploads: native `fetch()` with `FormData` - not `browserClient` (openapi-fetch breaks with multipart)
- Error handling: `getErrorMessage(error, fallback)` for simple errors, `getErrorMessage(error, fallback, messagesByCode)` / `getErrorCode()` to translate by backend `code` (never match `detail` text), `handleMutationError()` for forms with validation

## Styling
- `h-dvh` not `h-screen` for full-height layouts
- Content grids: `lg:grid-cols-2` (not `xl:`), page content: `max-w-7xl mx-auto`
- Buttons: default size with `w-full sm:w-auto`, right-aligned via `sm:justify-end`
- Animations with `motion-safe:` prefix

## TypeScript
- `noUncheckedIndexedAccess: true` - guard array/object index access

## i18n
- Keys: `{domain}_{feature}_{element}`, add to correct feature file in ALL locale directories
- Import: `import * as m from '$lib/paraglide/messages'`
- Paraglide module errors in svelte-check are expected (generated at build time) - ignore them
