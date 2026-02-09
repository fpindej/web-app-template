# Frontend Conventions (SvelteKit / Svelte 5)

> Follow the **Agent Workflow** in the root [`AGENTS.md`](../../AGENTS.md) — commit atomically, run quality checks before each commit, and write session docs when asked.

## Tech Stack

| Technology              | Purpose                                                   |
| ----------------------- | --------------------------------------------------------- |
| SvelteKit               | Framework (file-based routing, SSR, server routes)        |
| Svelte 5 (Runes)        | UI reactivity — `$state`, `$props`, `$derived`, `$effect` |
| TypeScript (strict)     | Type safety — no `any`, define proper interfaces          |
| Tailwind CSS 4          | Utility-first styling with CSS variable theming           |
| shadcn-svelte (bits-ui) | Headless, accessible UI component library                 |
| openapi-fetch           | Type-safe API client from generated OpenAPI types         |
| paraglide-js            | Type-safe i18n with compile-time message validation       |
| svelte-sonner           | Toast notifications                                       |

## Project Structure

```
src/
├── lib/
│   ├── api/                       # API client & error handling
│   │   ├── client.ts              # createApiClient(), browserClient
│   │   ├── error-handling.ts      # ProblemDetails parsing, field error mapping
│   │   ├── index.ts               # Barrel export
│   │   └── v1.d.ts                # ⚠️ GENERATED — never edit manually
│   │
│   ├── auth/                      # Authentication helpers
│   │   └── auth.ts                # getUser(), logout()
│   │
│   ├── components/
│   │   ├── ui/                    # shadcn components (generated, customizable)
│   │   ├── auth/                  # LoginForm, RegisterDialog
│   │   ├── layout/                # Header, Sidebar, UserNav, ThemeToggle
│   │   ├── profile/               # ProfileForm, AvatarDialog
│   │   └── common/                # StatusIndicator, WorkInProgress
│   │
│   ├── config/
│   │   ├── i18n.ts                # Language metadata (client-safe)
│   │   ├── index.ts               # Client-safe barrel — ⚠️ never export server config here
│   │   └── server.ts              # SERVER_CONFIG — import directly, not from barrel
│   │
│   ├── state/                     # Reactive state (.svelte.ts files only)
│   │   ├── shake.svelte.ts        # createShake(), createFieldShakes()
│   │   ├── shortcuts.svelte.ts    # Keyboard shortcuts
│   │   ├── sidebar.svelte.ts      # Sidebar state
│   │   └── theme.svelte.ts        # Theme (light/dark/system)
│   │
│   ├── types/
│   │   └── index.ts               # Type aliases from API schemas
│   │
│   └── utils/
│       ├── ui.ts                  # cn() for class merging
│       └── platform.ts            # IS_MAC, IS_WINDOWS detection
│
├── routes/
│   ├── (app)/                     # Authenticated (redirect to /login if no user)
│   │   ├── +layout.server.ts      # Auth guard
│   │   ├── +layout.svelte         # App shell (sidebar + header)
│   │   ├── profile/
│   │   └── settings/
│   │
│   ├── (public)/                  # Unauthenticated
│   │   └── login/
│   │
│   ├── api/                       # API proxy routes
│   │   ├── [...path]/+server.ts   # Catch-all proxy to backend
│   │   └── health/+server.ts      # Health check proxy
│   │
│   ├── +layout.svelte             # Root layout (theme init, shortcuts, toast)
│   ├── +layout.server.ts          # Root server load (fetch user, locale)
│   └── +error.svelte              # Error page with status-aware icons
│
├── messages/                      # i18n translation files
│   ├── en.json
│   └── cs.json
│
└── styles/                        # Global CSS
    ├── index.css                  # Entry point — imports all modules
    ├── themes.css                 # CSS variables (:root + .dark)
    ├── tailwind.css               # @theme inline mappings
    ├── base.css                   # @layer base styles
    ├── animations.css             # Keyframes + animation utilities
    └── utilities.css              # Reusable effect classes
```

## API Type Generation

Types are auto-generated from the backend's OpenAPI specification. **Never hand-edit `v1.d.ts`.**

### Regenerate Types

```bash
npm run api:generate
```

This fetches `/openapi/v1.json` from the running backend and generates `src/lib/api/v1.d.ts`. The backend must be running.

### Using Generated Types

Response types are inferred automatically through the API client:

```typescript
const { data } = await browserClient.GET('/api/users/me');
// data is typed as UserResponse | undefined
```

For explicit type imports:

```typescript
import type { components } from '$lib/api/v1';
type User = components['schemas']['UserResponse'];
```

Create type aliases in `$lib/types/index.ts` for commonly used schemas:

```typescript
import type { components } from '$lib/api/v1';
export type User = components['schemas']['UserResponse'];
```

### After Regenerating

1. Review changes in `v1.d.ts` for breaking changes
2. Update any affected API calls
3. Run `npm run check` to catch type errors

### Missing API Endpoints

If the backend doesn't provide data you need, **don't work around it**. Since we control the full stack:

1. Describe what you need (HTTP method, path, request/response shape)
2. Propose the backend endpoint
3. Wait for confirmation before implementing frontend workarounds

## API Client

### Architecture

`createApiClient()` wraps `openapi-fetch` with automatic 401 → refresh → retry logic:

1. Any request returns 401 → trigger `POST /api/auth/refresh` (once, shared across concurrent requests)
2. If refresh succeeds → retry the original request with new cookies
3. If refresh fails → return the 401 to the caller

Two client variants:

| Client          | Created With                         | Use In                                                 |
| --------------- | ------------------------------------ | ------------------------------------------------------ |
| `browserClient` | `createApiClient()`                  | Client-side code (components, event handlers)          |
| Server client   | `createApiClient(fetch, url.origin)` | `+page.server.ts` / `+layout.server.ts` load functions |

### Server-Side (Recommended for Initial Load)

Use `+page.server.ts` for data needed on page render:

```typescript
import { createApiClient } from '$lib/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const client = createApiClient(fetch, url.origin);
	const { data } = await client.GET('/api/users/me');
	return { user: data };
};
```

### Client-Side (For User Interactions)

Use `browserClient` for form submissions and user-triggered actions:

```typescript
import { browserClient } from '$lib/api';

const { data, response, error } = await browserClient.PATCH('/api/users/me', {
	body: { firstName, lastName }
});
```

### When to Use Which

| Pattern                         | Use For                                                     |
| ------------------------------- | ----------------------------------------------------------- |
| Server-side (`+page.server.ts`) | Initial page data, SEO content, data requiring auth cookies |
| Client-side (`browserClient`)   | Form submissions, user actions, polling, post-load updates  |

### API Proxy

All `/api/*` requests are proxied to the backend via `routes/api/[...path]/+server.ts`. This catch-all route forwards cookies and headers automatically. It handles `ECONNREFUSED` by returning 503 "Backend unavailable".

## Error Handling

### Validation Errors (Field-Level)

ASP.NET Core returns `ValidationProblemDetails` with field-level errors. Handle them with the provided utilities:

```typescript
import { isValidationProblemDetails, mapFieldErrors, browserClient } from '$lib/api';
import { createFieldShakes } from '$lib/state';
import { toast } from '$lib/components/ui/sonner';
import * as m from '$lib/paraglide/messages';

const fieldShakes = createFieldShakes();
let fieldErrors = $state<Record<string, string>>({});

async function handleSubmit() {
	fieldErrors = {};
	const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
		body: { firstName, lastName }
	});

	if (response.ok) {
		toast.success(m.profile_updateSuccess());
	} else if (isValidationProblemDetails(apiError)) {
		fieldErrors = mapFieldErrors(apiError.errors); // PascalCase → camelCase
		fieldShakes.triggerFields(Object.keys(fieldErrors));
	} else {
		toast.error(getErrorMessage(apiError, m.profile_updateError()));
	}
}
```

`mapFieldErrors` converts ASP.NET Core's PascalCase field names to camelCase (e.g., `PhoneNumber` → `phoneNumber`). The default mapping is in `error-handling.ts`; extend it for new fields via the `customFieldMap` parameter.

### Network Errors

The API proxy handles network errors:

```typescript
if (isFetchErrorWithCode(err, 'ECONNREFUSED')) {
	return new Response(JSON.stringify({ message: 'Backend unavailable' }), { status: 503 });
}
```

## Security

### Principle: Restrictive by Default

Always default to the most restrictive security posture and only relax constraints when a feature explicitly requires it.

### Security Response Headers

The `handle` hook in `hooks.server.ts` adds security headers to all page responses. API proxy routes (`/api/*`) are skipped — those receive headers from the backend.

| Header                   | Value                                      | Purpose                                     |
| ------------------------ | ------------------------------------------ | ------------------------------------------- |
| `X-Content-Type-Options` | `nosniff`                                  | Prevents MIME-type sniffing                 |
| `X-Frame-Options`        | `DENY`                                     | Prevents iframe embedding (clickjacking)    |
| `Referrer-Policy`        | `strict-origin-when-cross-origin`          | Prevents leaking URL paths to third parties |
| `Permissions-Policy`     | `camera=(), microphone=(), geolocation=()` | Disables unused browser APIs                |

`Permissions-Policy` directives use `()` (empty allowlist) to deny access entirely. If a feature needs a browser API (e.g., webcam for avatar capture), change the specific directive to `(self)` — never remove the header or use `*`.

### CSRF Protection

The API proxy at `routes/api/[...path]/+server.ts` validates the `Origin` header on state-changing requests (POST/PUT/PATCH/DELETE). Cross-origin requests are rejected with 403. This complements SvelteKit's built-in CSRF protection, which only covers form actions — not `+server.ts` routes.

## Svelte 5 Patterns

**Runes only.** Never use `export let` — always `$props()`.

### Component Props

```svelte
<script lang="ts">
	interface Props {
		user: User;
		onSave?: (data: FormData) => void;
		class?: string;
	}

	let { user, onSave, class: className }: Props = $props();
</script>
```

### Reactive State

```svelte
<script lang="ts">
	let count = $state(0);
	let items = $state<string[]>([]);
	let doubled = $derived(count * 2);

	$effect(() => {
		console.log('Count changed:', count);
	});
</script>
```

### Bindable Props

```svelte
<script lang="ts">
	let { open = $bindable() }: { open: boolean } = $props();
</script>

<!-- Usage: <Dialog bind:open={isOpen} /> -->
```

### Snippets (Replacing Slots)

```svelte
<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		header?: Snippet;
		content?: Snippet;
	}

	let { header, content }: Props = $props();
</script>

<div>
	{#if header}{@render header()}{/if}
	{#if content}{@render content()}{/if}
</div>
```

## Component Organization

### Feature Folders

Components live in feature folders under `$lib/components/`:

```
components/
├── auth/            # LoginForm, RegisterDialog
│   └── index.ts     # Barrel export
├── profile/         # ProfileForm, AvatarDialog
│   └── index.ts
├── layout/          # Header, Sidebar, ThemeToggle
│   └── index.ts
├── common/          # StatusIndicator, WorkInProgress
│   └── index.ts
└── ui/              # shadcn (generated)
```

### Barrel Exports

Every feature folder has an `index.ts` that re-exports all components:

```typescript
// $lib/components/profile/index.ts
export { default as ProfileForm } from './ProfileForm.svelte';
export { default as AvatarDialog } from './AvatarDialog.svelte';
```

### Import Rules

```typescript
// ✅ Always use barrel exports
import { ProfileForm, AvatarDialog } from '$lib/components/profile';
import { createFieldShakes } from '$lib/state';
import { cn } from '$lib/utils';

// ❌ Never import directly from files
import ProfileForm from '$lib/components/profile/ProfileForm.svelte';

// ⚠️ Server config — import directly, not from barrel
import { SERVER_CONFIG } from '$lib/config/server';
```

### Adding shadcn Components

```bash
npx shadcn-svelte@next add <component-name>
```

Components are generated in `$lib/components/ui/`. Do not manually create components that shadcn already provides.

## Reactive State

State files use `.svelte.ts` extension and live in `$lib/state/`:

| File                  | Exports                                                                 |
| --------------------- | ----------------------------------------------------------------------- |
| `shake.svelte.ts`     | `createShake()`, `createFieldShakes()` — field-level animation triggers |
| `theme.svelte.ts`     | `getTheme()`, `setTheme()`, `toggleTheme()` — light/dark/system         |
| `sidebar.svelte.ts`   | `sidebarState`, `toggleSidebar()`, `setSidebarCollapsed()`              |
| `shortcuts.svelte.ts` | `shortcuts` action, `getShortcutDisplay()` — keyboard shortcuts         |

**Never** mix reactive state (`.svelte.ts`) with pure utilities (`.ts`).

## Internationalization (i18n)

### Key Naming Convention

```
{domain}_{feature}_{element}
```

Examples: `auth_login_title`, `profile_personalInfo_firstName`, `nav_dashboard`, `meta_profile_title`

### Usage

```svelte
<script lang="ts">
	import * as m from '$lib/paraglide/messages';
</script>

<h1>{m.auth_login_title()}</h1>
<Label>{m.profile_personalInfo_firstName()}</Label>

<svelte:head>
	<title>{m.meta_profile_title()}</title>
</svelte:head>
```

### Adding Keys

Edit both `src/messages/en.json` and `src/messages/cs.json`:

```json
{ "profile_newFeature_label": "New Feature" }
```

## Styling

### CSS Architecture

Styles are modular in `src/styles/`:

| File             | Purpose                              | Edit When                        |
| ---------------- | ------------------------------------ | -------------------------------- |
| `themes.css`     | HSL color tokens (`:root` + `.dark`) | Adding new color variables       |
| `tailwind.css`   | `@theme inline` mappings to CSS vars | Extending Tailwind design tokens |
| `base.css`       | `@layer base` element styles         | Global element resets            |
| `animations.css` | Keyframes + animation classes        | Adding new animations            |
| `utilities.css`  | Reusable effect classes              | Glow effects, card hovers        |

### Adding a Theme Variable

```css
/* 1. Define in themes.css */
:root { --accent: 210 40% 50%; }
.dark { --accent: 210 40% 60%; }

/* 2. Map in tailwind.css */
@theme inline { --color-accent: hsl(var(--accent)); }

/* 3. Use in components */
<div class="bg-accent text-accent-foreground">
```

### Logical Properties Only (RTL Support)

```html
<!-- ✅ Correct -->
<div class="ms-4 me-2 ps-3 pe-1 text-start">
	<!-- ❌ Wrong (breaks RTL) -->
	<div class="mr-2 ml-4 pr-1 pl-3 text-left"></div>
</div>
```

| Physical                   | Logical                   |
| -------------------------- | ------------------------- |
| `ml-*` / `mr-*`            | `ms-*` / `me-*`           |
| `pl-*` / `pr-*`            | `ps-*` / `pe-*`           |
| `left-*` / `right-*`       | `start-*` / `end-*`       |
| `text-left` / `text-right` | `text-start` / `text-end` |

### Class Merging

Use `cn()` from `$lib/utils` to merge Tailwind classes:

```svelte
<button class={cn('rounded px-4 py-2', variant === 'destructive' && 'bg-red-500', className)}>
```

### Reduced Motion

Always respect `prefers-reduced-motion`:

```html
<div class="motion-safe:duration-300 motion-safe:animate-in motion-safe:fade-in"></div>
```

For custom CSS animations in `animations.css`, add a `prefers-reduced-motion: reduce` media query that disables them.

## Route Structure & Data Fetching

### Authentication Guard

The `(app)` layout checks for a user and redirects to `/login`:

```typescript
// routes/(app)/+layout.server.ts
export const load: LayoutServerLoad = async ({ parent }) => {
	const { user } = await parent();
	if (!user) throw redirect(303, '/login');
	return { user };
};
```

### Root Layout Data

The root `+layout.server.ts` fetches the user and locale for all routes. In development, it also exposes the internal API URL for debugging (visible in the login page status indicator):

```typescript
export const load: LayoutServerLoad = async ({ locals, fetch, url }) => {
	const user = await getUser(fetch, url.origin);
	return { user, locale: locals.locale, apiUrl: dev ? SERVER_CONFIG.API_URL : undefined };
};
```

### Combining Server + Client Data

Load initial data server-side, then update client-side:

```svelte
<script lang="ts">
	import { browserClient } from '$lib/api';

	let { data } = $props();
	let user = $state(data.user);

	async function refresh() {
		const { data: updated } = await browserClient.GET('/api/users/me');
		if (updated) user = updated;
	}
</script>
```

## Quality Checklist

Run **all** of these before every commit:

```bash
npm run format   # Prettier
npm run lint     # ESLint
npm run check    # Svelte + TypeScript type check
npm run build    # Production build (run occasionally, always before PR)
```

## Don'ts

- ❌ `export let` — use `$props()`
- ❌ `any` type — define proper interfaces
- ❌ Physical CSS properties (`ml-`, `mr-`, `left-`, `right-`)
- ❌ Import server config from barrel (`$lib/config`)
- ❌ Leave components in `$lib/components/` root — use feature folders
- ❌ Mix reactive state (`.svelte.ts`) with pure utils (`.ts`)
- ❌ Hand-edit `v1.d.ts` — run `npm run api:generate`
- ❌ Create UI components that shadcn already provides
- ❌ Work around missing API endpoints — propose them instead

## Adding a New Feature — Checklist

1. **Types**: Run `npm run api:generate` if backend has new endpoints
2. **Type alias**: Add to `$lib/types/index.ts` if the schema is commonly used
3. **Components**: Create in `$lib/components/{feature}/` with barrel `index.ts`
4. **State**: If needed, create `$lib/state/{feature}.svelte.ts`
5. **Route**: Create page in `routes/(app)/{feature}/`
6. **Server load**: Add `+page.server.ts` for initial data
7. **i18n**: Add keys to both `en.json` and `cs.json`
8. **Navigation**: Update sidebar/header if adding a new page

Commit atomically: types+aliases → components → route+server-load → i18n keys.
