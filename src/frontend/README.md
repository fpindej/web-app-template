# SvelteKit Frontend Template

A production-ready SvelteKit template with Svelte 5, TypeScript, Tailwind CSS 4, and shadcn-svelte.

## Tech Stack

| Layer     | Technology                   |
| --------- | ---------------------------- |
| Framework | SvelteKit + Svelte 5 (Runes) |
| Language  | TypeScript (Strict)          |
| Styling   | Tailwind CSS 4               |
| UI        | shadcn-svelte (bits-ui)      |
| i18n      | Paraglide JS                 |
| API       | openapi-fetch (type-safe)    |

## Getting Started

### Prerequisites

- Node.js 20+
- Backend API running (for API type generation)

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

### Generate API Types

When the backend API changes, regenerate the TypeScript types:

```bash
npm run api:generate
```

This requires the backend to be running (serves OpenAPI spec).

### Build

```bash
npm run build
```

## Project Structure

```
src/
├── lib/
│   ├── api/           # API client, error handling, generated types
│   ├── auth/          # Authentication utilities
│   ├── components/    # UI components (grouped by feature)
│   │   ├── ui/        # shadcn base components
│   │   ├── auth/      # Login, Register components
│   │   ├── layout/    # Header, Sidebar, Navigation
│   │   ├── profile/   # Profile page components
│   │   └── common/    # Shared components
│   ├── config/        # Configuration (i18n, server)
│   ├── state/         # Reactive state (.svelte.ts files)
│   ├── types/         # Type definitions
│   └── utils/         # Pure utility functions
├── messages/          # i18n translation files (en.json, cs.json)
└── routes/
    ├── (app)/         # Authenticated routes
    ├── (public)/      # Public routes (login)
    └── api/           # API proxy routes
```

### Feature-Based Organization

Components and features are grouped by domain, making it easy to:

- **Add features**: Create a new folder under `components/` with related components
- **Remove features**: Delete the folder and its references (routes, translations, imports)

For example, to remove the profile feature entirely:

1. Delete `src/lib/components/profile/`
2. Delete `src/routes/(app)/profile/`
3. Remove profile-related keys from `src/messages/*.json`
4. Remove any imports referencing profile components

## Working with the Codebase

### Key Patterns

**Svelte 5 Runes** - Use `$state`, `$props`, `$derived`, `$effect`:

```svelte
<script lang="ts">
	let { user }: { user: User } = $props();
	let count = $state(0);
	let doubled = $derived(count * 2);
</script>
```

**API Calls** - Use the type-safe client:

```typescript
import { browserClient } from '$lib/api';

const { data, error } = await browserClient.GET('/api/users/me');
```

**Translations** - Use Paraglide with type-safe keys:

```svelte
<script lang="ts">
	import * as m from '$lib/paraglide/messages';
</script>

<h1>{m.auth_login_title()}</h1>
```

**Styling** - Use Tailwind with logical properties for RTL support:

```html
<!-- Use logical properties -->
<div class="ms-4 ps-2 text-start">
	<!-- Avoid physical properties -->
	<div class="ml-4 pl-2 text-left"></div>
</div>
```

✅ Logical: `ms-*`, `me-*`, `ps-*`, `pe-*`, `text-start`  
❌ Physical: `ml-*`, `mr-*`, `pl-*`, `pr-*`, `text-left`

### Adding UI Components

Check [shadcn-svelte](https://shadcn-svelte.com/) before creating custom components:

```bash
npx shadcn-svelte@next add <component-name>
```

### Import Conventions

```typescript
// Use barrel exports
import { Header, Sidebar } from '$lib/components/layout';
import { browserClient } from '$lib/api';

// Don't import directly from files
import Header from '$lib/components/layout/Header.svelte';
```

✅ Use barrel exports from `index.ts`  
❌ Don't import directly from `.svelte` files

## Quality Checks

Before committing, run all checks:

```bash
npm run format   # Fix formatting
npm run lint     # Check lint errors
npm run check    # TypeScript/Svelte check
npm run build    # Verify build
```

## AI Assistant Instructions

For detailed coding conventions, patterns, and best practices, see [`.github/copilot-instructions.md`](.github/copilot-instructions.md). This file provides comprehensive guidance for AI assistants (and developers) working on this codebase.

## Available Scripts

| Script         | Description                          |
| -------------- | ------------------------------------ |
| `dev`          | Start development server             |
| `build`        | Create production build              |
| `preview`      | Preview production build             |
| `check`        | Run Svelte/TypeScript checks         |
| `lint`         | Run ESLint and Prettier checks       |
| `format`       | Format code with Prettier            |
| `api:generate` | Generate API types from OpenAPI spec |
