# Frontend Conventions (SvelteKit / Svelte 5)

> Follow the **Agent Workflow** in the root [`AGENTS.md`](../../AGENTS.md) — commit atomically, run quality checks before each commit, and write session docs when asked.
>
> For explanations, rationale, and design decisions, see [`docs/frontend-conventions.md`](../../docs/frontend-conventions.md) and [`docs/security.md`](../../docs/security.md).

## Tech Stack

| Technology              | Purpose                                                   |
| ----------------------- | --------------------------------------------------------- |
| SvelteKit               | Framework (file-based routing, SSR, server routes)        |
| Svelte 5 (Runes)        | UI reactivity — `$state`, `$props`, `$derived`, `$effect` |
| TypeScript (strict)     | Type safety — no `any`, define proper interfaces          |
| Tailwind CSS 4          | Utility-first styling with CSS variable theming           |
| shadcn-svelte (bits-ui) | Headless, accessible UI component library                 |
| tailwind-variants       | Variant-based styling for complex components (e.g. sheet) |
| openapi-fetch           | Type-safe API client from generated OpenAPI types         |
| paraglide-js            | Type-safe i18n with compile-time message validation       |
| svelte-sonner           | Toast notifications                                       |
| @internationalized/date | Locale-aware date formatting                              |
| flag-icons              | Country flag CSS sprites (phone input)                    |

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
│   ├── assets/                    # Static assets (favicon, images)
│   │   └── favicon.svg
│   │
│   ├── auth/                      # Authentication helpers
│   │   └── auth.ts                # getUser(), logout()
│   │
│   ├── components/
│   │   ├── ui/                    # shadcn components (generated, customizable)
│   │   ├── auth/                  # LoginForm, LoginBackground, RegisterDialog
│   │   ├── getting-started/       # GettingStarted, markdown renderer (removable)
│   │   ├── layout/                # Header, Sidebar, SidebarNav, UserNav,
│   │   │                          # ThemeToggle, LanguageSelector, ShortcutsHelp
│   │   ├── profile/               # ProfileForm, ProfileHeader, AvatarDialog,
│   │   │                          # AccountDetails, InfoItem
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
│       ├── platform.ts            # IS_MAC, IS_WINDOWS detection
│       └── index.ts               # Barrel export (cn, WithoutChildrenOrChild)
│
├── routes/
│   ├── (app)/                     # Authenticated (redirect to /login if no user)
│   │   ├── +layout.server.ts      # Auth guard
│   │   ├── +layout.svelte         # App shell (sidebar + header)
│   │   ├── +page.svelte           # Dashboard / Getting Started
│   │   ├── analytics/             # Analytics page (WIP placeholder)
│   │   ├── profile/               # User profile page
│   │   ├── reports/               # Reports page (WIP placeholder)
│   │   └── settings/              # Settings page (WIP placeholder)
│   │
│   ├── (public)/                  # Unauthenticated
│   │   └── login/
│   │       ├── +page.server.ts    # Redirect to / if already logged in
│   │       └── +page.svelte       # Login page
│   │
│   ├── api/                       # API proxy routes
│   │   ├── [...path]/+server.ts   # Catch-all proxy to backend
│   │   └── health/+server.ts      # Health check proxy
│   │
│   ├── +layout.svelte             # Root layout (theme init, shortcuts, toast)
│   ├── +layout.server.ts          # Root server load (fetch user, locale)
│   ├── +layout.ts                 # Universal load (set paraglide locale)
│   └── +error.svelte              # Error page with status-aware icons
│
├── messages/                      # i18n translation files
│   ├── en.json
│   └── cs.json
│
└── styles/                        # Global CSS (modular architecture)
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

This fetches `/openapi/v1.json` from the running backend and generates `src/lib/api/v1.d.ts`. The backend must be running (either in Docker or from IDE).

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

`createApiClient()` wraps `openapi-fetch` with automatic 401 → refresh → retry logic. Concurrent 401s share a single refresh promise.

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

All `/api/*` requests are proxied to the backend via `routes/api/[...path]/+server.ts`. Handles `ECONNREFUSED` by returning 503.

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

`mapFieldErrors` converts ASP.NET Core's PascalCase field names to camelCase. Extend via the `customFieldMap` parameter for new fields.

### Network Errors

The API proxy handles `ECONNREFUSED` → 503 "Backend unavailable".

## Security

> For security architecture, CSP rationale, and CSRF design, see [`docs/security.md`](../../docs/security.md).

### Security Response Headers

The `handle` hook in `hooks.server.ts` adds security headers to page responses. API proxy routes (`/api/*`) are skipped.

| Header                   | Value                                      | Purpose                                     |
| ------------------------ | ------------------------------------------ | ------------------------------------------- |
| `X-Content-Type-Options` | `nosniff`                                  | Prevents MIME-type sniffing                 |
| `X-Frame-Options`        | `DENY`                                     | Prevents iframe embedding (clickjacking)    |
| `Referrer-Policy`        | `strict-origin-when-cross-origin`          | Prevents leaking URL paths to third parties |
| `Permissions-Policy`     | `camera=(), microphone=(), geolocation=()` | Disables unused browser APIs                |

`Permissions-Policy` uses `()` to deny. To enable a browser API, change the specific directive to `(self)`.

### Content Security Policy (CSP)

CSP is configured via nonce mode in `svelte.config.js`:

```js
kit: {
	csp: {
		directives: {
			'script-src': ['self', 'nonce'],
			'style-src': ['self', 'unsafe-inline'],   // Required for Svelte transitions
			'img-src': ['self', 'https:', 'data:'],    // data: required for Vite-inlined assets
			'frame-ancestors': ['none']
		}
	}
}
```

Key rules:

- **`script-src`**: Nonce-based. FOUC script in `app.html` uses `%sveltekit.nonce%`.
- **`style-src`**: `'unsafe-inline'` required for Svelte transitions.
- **`img-src`**: `data:` required for Vite-inlined assets (<4KB files).
- **`frame-ancestors`**: `'none'` — defense-in-depth with `X-Frame-Options: DENY`.

CSP is set by SvelteKit automatically. `hooks.server.ts` does NOT set CSP.

### HSTS

Production-only in `hooks.server.ts`:

```typescript
if (!dev) {
	response.headers.set('Strict-Transport-Security', 'max-age=63072000; includeSubDomains');
}
```

### CSRF Protection

The API proxy validates `Origin` on state-changing requests (POST/PUT/PATCH/DELETE). Allows:

1. **Same-origin requests** — `Origin` matches `url.origin` (the SvelteKit server's own origin)
2. **Configured origins** — `Origin` matches an entry in `ALLOWED_ORIGINS` (env var, comma-separated)
3. **Missing `Origin` header** — safe to allow (same-origin older browsers or non-browser clients)

To allow access through a reverse proxy or tunnel, set `ALLOWED_ORIGINS`:

```bash
ALLOWED_ORIGINS=https://abc123.ngrok-free.app
```

## Svelte 5 Patterns

**Runes only.** Never use `export let` — always `$props()`.

### Component Props

Always use `interface Props` and destructure from `$props()`:

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

Never use the generic syntax `$props<{ ... }>()` — always define a separate `interface Props`.

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
├── auth/            # LoginForm, LoginBackground, RegisterDialog
│   └── index.ts     # Barrel export
├── getting-started/ # GettingStarted, markdown.ts (removable starter page)
│   └── index.ts
├── profile/         # ProfileForm, ProfileHeader, AvatarDialog,
│   └── index.ts     # AccountDetails, InfoItem
├── layout/          # Header, Sidebar, SidebarNav, UserNav,
│   └── index.ts     # ThemeToggle, LanguageSelector, ShortcutsHelp
├── common/          # StatusIndicator, WorkInProgress
│   └── index.ts
└── ui/              # shadcn (generated, customizable)
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

This generates components in `$lib/components/ui/<component>/`. The configuration lives in `components.json` at the frontend root:

```json
{
	"style": "default",
	"tailwind": {
		"config": "vite.config.ts",
		"css": "src/styles/index.css",
		"baseColor": "slate",
		"cssVariables": true
	},
	"aliases": {
		"components": "$lib/components",
		"utils": "$lib/utils",
		"ui": "$lib/components/ui"
	}
}
```

**Rules for shadcn components:**

- Do not manually create components that shadcn already provides — use the CLI.
- Generated components are **customizable** (this is a template, not a library). Modifying them is acceptable and expected.
- When touching any shadcn component, convert physical CSS properties to logical (see Styling section).
- When adding i18n to shadcn components (e.g., localizing "Close" sr-only text), import `$lib/paraglide/messages` and use message functions.
- Available components: alert, avatar, badge, button, card, dialog, dropdown-menu, input, label, phone-input (custom), sheet, sonner, textarea, tooltip.
- Browse the full catalog at [ui.shadcn.com](https://ui.shadcn.com) to find components before building custom ones.

## Reactive State

State files use `.svelte.ts` extension in `$lib/state/`:

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

### Paraglide Module Resolution

Paraglide generates `$lib/paraglide/*` at build time. `svelte-check` shows ~32 errors about these modules — **not real errors**. They disappear after `npm run build`.

## Styling

### CSS Architecture

Styles are modular in `src/styles/`. The entry point is `index.css`, which imports everything in order:

| File             | Purpose                                     | Edit When                        |
| ---------------- | ------------------------------------------- | -------------------------------- |
| `index.css`      | Entry point, Tailwind base + plugin imports | Rarely — adding new CSS modules  |
| `themes.css`     | HSL color tokens (`:root` + `.dark`)        | Adding new color variables       |
| `tailwind.css`   | `@theme inline` mappings to CSS vars        | Extending Tailwind design tokens |
| `base.css`       | `@layer base` element styles                | Global element resets            |
| `animations.css` | Keyframes + animation classes               | Adding new animations            |
| `utilities.css`  | Reusable effect classes                     | Glow effects, card hovers        |

Tailwind CSS 4 is configured via the Vite plugin — no `tailwind.config.js` or `postcss.config.js`.

Dark mode uses a class-based strategy with a custom variant:

```css
@custom-variant dark (&:where(.dark, .dark *));
```

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

**All CSS must use logical properties.** Physical directional properties break RTL layouts.

```html
<!-- ✅ Correct -->
<div class="ms-4 me-2 ps-3 pe-1 text-start">
	<!-- ❌ Wrong (breaks RTL) -->
	<div class="mr-2 ml-4 pr-1 pl-3 text-left"></div>
</div>
```

| Physical                      | Logical                          |
| ----------------------------- | -------------------------------- |
| `ml-*` / `mr-*`               | `ms-*` / `me-*`                  |
| `pl-*` / `pr-*`               | `ps-*` / `pe-*`                  |
| `left-*` / `right-*`          | `start-*` / `end-*`              |
| `text-left` / `text-right`    | `text-start` / `text-end`        |
| `border-l` / `border-r`       | `border-s` / `border-e`          |
| `rounded-l-*` / `rounded-r-*` | `rounded-s-*` / `rounded-e-*`    |
| `float-left` / `float-right`  | `float-start` / `float-end`      |
| `space-x-*` (on flex/grid)    | `gap-*` (preferred on flex/grid) |

**Note:** `space-x-*` uses `margin-left` internally. On flex/grid containers, prefer `gap-*`.

**Note:** Animation classes (`slide-in-from-left`, etc.) are animation names, not physical properties — acceptable.

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

### Responsive Design

**Mobile-first.** Start with the smallest viewport and add breakpoints for larger screens.

#### Breakpoints

| Prefix | Min Width | Target           |
| ------ | --------- | ---------------- |
| (none) | 0         | Mobile (default) |
| `sm:`  | 640px     | Large phone      |
| `md:`  | 768px     | Tablet           |
| `lg:`  | 1024px    | Laptop           |
| `xl:`  | 1280px    | Desktop          |
| `2xl:` | 1536px    | Wide desktop     |

#### Mandatory Rules

1. **Always start mobile-first.** Write base styles for 320px, then add `sm:`, `md:`, `lg:` prefixes for larger screens.

2. **Never use multi-column grids inside dialogs without a mobile fallback.**

   ```html
   <!-- ✅ Correct -->
   <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
   	<!-- ❌ Wrong — unusable on mobile inside a dialog -->
   	<div class="grid grid-cols-2 gap-4"></div>
   </div>
   ```

3. **Scale padding responsively.** Never use large flat padding values.

   ```html
   <!-- ✅ Correct -->
   <div class="p-4 sm:p-6 lg:p-8">
   	<!-- ❌ Wrong — 64px padding on a 320px screen -->
   	<div class="p-16"></div>
   </div>
   ```

4. **Minimum font size: 12px (`text-xs`).** Never use `text-[10px]` or smaller — it's unreadable on mobile and fails WCAG.

5. **Touch targets: minimum 40px (h-10).** All interactive elements (buttons, links, toggles) must have at least 40px height. Prefer 44px (`h-11`) for primary actions. Inline text buttons need `min-h-10` with `inline-flex items-center`.

6. **Use `h-dvh` not `h-screen`** for full-height layouts. `h-dvh` (dynamic viewport height) accounts for mobile browser chrome (address bar, bottom nav).

7. **Prevent flex overflow.** Add `min-w-0` on flex children that contain text that might overflow. Add `truncate` or `overflow-hidden` when content must not wrap.

8. **`overflow-hidden` on scroll containers** to prevent horizontal page scroll on mobile.

9. **Use `shrink-0`** on elements that must not compress (icons, badges, buttons alongside text).

#### Test Viewports

When making responsive changes, mentally verify at these widths: **320px**, **375px**, **768px**, **1024px**, **1440px**.

#### Existing Responsive Patterns (Reference)

The app layout uses a mobile-first sidebar pattern:

- `md:hidden` — mobile hamburger menu (sheet drawer)
- `hidden md:block` — desktop sidebar
- `h-dvh` — full dynamic viewport height
- Feature grids: `sm:grid-cols-2 xl:grid-cols-4`
- Dialog footers: `flex-col-reverse sm:flex-row`
- Text sizes: `text-sm sm:text-base md:text-lg`

## Route Structure & Data Fetching

> For the full authentication flow diagram and data fetching strategy, see [`docs/frontend-conventions.md`](../../docs/frontend-conventions.md#route-structure) and [`docs/architecture.md`](../../docs/architecture.md).

### Route Groups

| Group      | Path                                   | Guard                                   | Purpose               |
| ---------- | -------------------------------------- | --------------------------------------- | --------------------- |
| `(app)`    | `/*` (dashboard, profile, settings...) | Requires authenticated user             | All protected pages   |
| `(public)` | `/login`                               | Redirects away if already authenticated | Unauthenticated pages |
| `api`      | `/api/*`                               | CSRF origin validation                  | API proxy to backend  |

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

### Adding a New Guarded Route Group

1. Create route group directory with `+layout.server.ts`
2. Add a layout guard:

   ```typescript
   // routes/(admin)/+layout.server.ts
   import { redirect } from '@sveltejs/kit';
   import type { LayoutServerLoad } from './$types';

   export const load: LayoutServerLoad = async ({ parent }) => {
   	const { user } = await parent();
   	if (!user) throw redirect(303, '/login');
   	if (!user.roles?.includes('Admin')) throw redirect(303, '/');
   	return { user };
   };
   ```

3. **Always enforce authorization on the backend too** — frontend guards are UX, not security.

### Role-Based Access

| Level | Mechanism | Purpose |
|---|---|---|
| **Backend** (authoritative) | `[Authorize(Roles = "...")]` | Security — rejects with 403 |
| **Frontend** (UX) | Layout guards + conditional rendering | Prevents seeing unusable UI |

#### Conditional Rendering by Role

```svelte
<script lang="ts">
	import type { User } from '$lib/types';

	interface Props {
		user: User;
	}

	let { user }: Props = $props();
</script>

{#if user.roles?.includes('Admin')}
	<a href="/admin-panel">Admin Panel</a>
{/if}
```

### Root Layout Data

The root `+layout.server.ts` fetches user and locale for all routes. In dev, exposes API URL for debugging:

```typescript
export const load: LayoutServerLoad = async ({ locals, fetch, url }) => {
	const user = await getUser(fetch, url.origin);
	return { user, locale: locals.locale, apiUrl: dev ? SERVER_CONFIG.API_URL : undefined };
};
```

### Universal Load Function

The root `+layout.ts` sets paraglide locale from server data:

```typescript
export const load: LayoutLoad = async ({ data }) => {
	const locale = data.locale;
	if (locales.includes(locale)) {
		setLocale(locale);
	} else {
		setLocale(baseLocale);
	}
	return data;
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

### FOUC Prevention

`app.html` contains an inline `<script>` that applies the `.dark` class from `localStorage` before paint.

### Raw File Imports

Use Vite's `?raw` suffix to import file contents as strings:

```typescript
import readmeContent from '../../../../README.md?raw';
```

## TypeScript Patterns

### Type Narrowing Over Assertions

Prefer type narrowing over `as` casts:

```typescript
// ✅ Correct
if ('detail' in error) {
	// TypeScript narrows the type
}

// ❌ Avoid
const detail = (error as ApiError).detail;
```

### localStorage Access

Always wrap `localStorage` access in `try/catch` — it throws in private browsing mode and when storage quota is exceeded:

```typescript
// ✅ Correct
try {
	const value = localStorage.getItem('key');
} catch {
	// Ignore — private browsing or quota exceeded
}
```

### Navigator API

Prefer `navigator.userAgentData.platform` (modern Chromium) with `navigator.platform` fallback:

```typescript
const platform = navigator.userAgentData?.platform ?? navigator.platform;
```

### Navigation with `resolve()`

Always use `resolve()` from `$app/paths` with `goto()` for base-path-aware navigation:

```typescript
import { goto } from '$app/navigation';
import { resolve } from '$app/paths';

await goto(resolve('/'));
```

Never suppress the `svelte/no-navigation-without-resolve` lint rule.

## Quality Checklist

Run **all** of these before every commit:

```bash
npm run format   # Prettier
npm run lint     # ESLint
npm run check    # Svelte + TypeScript type check
```

Run occasionally (always before PR):

```bash
npm run build    # Production build
```

### Known `svelte-check` Errors

~32 errors about `$lib/paraglide/*` module resolution are expected (see [Paraglide Module Resolution](#paraglide-module-resolution)).

## Don'ts

- `export let` — use `$props()`
- `$props<{ ... }>()` generic syntax — use `interface Props` + `$props()`
- `any` type — define proper interfaces
- `as` type assertions when narrowing is possible
- Physical CSS properties (`ml-`, `mr-`, `left-`, `right-`, `border-l`, `border-r`)
- `space-x-*` on flex/grid — use `gap-*` instead
- `text-[10px]` or smaller — minimum `text-xs` (12px)
- `p-16` or large flat padding — scale responsively (`p-4 sm:p-6 lg:p-8`)
- `grid-cols-2+` inside dialogs without `grid-cols-1` mobile fallback
- `h-screen` — use `h-dvh` for full-height layouts
- `null!` non-null assertions
- Import server config from barrel (`$lib/config`)
- Leave components in `$lib/components/` root — use feature folders
- Mix reactive state (`.svelte.ts`) with pure utils (`.ts`)
- Hand-edit `v1.d.ts` — run `npm run api:generate`
- Create UI components that shadcn already provides
- Work around missing API endpoints — propose them instead
- Suppress `svelte/no-navigation-without-resolve` — use `resolve()` with `goto()`
- Silent failures — always handle errors explicitly

## Adding a New Feature — Checklist

1. **Types**: Run `npm run api:generate` if backend has new endpoints
2. **Type alias**: Add to `$lib/types/index.ts` if the schema is commonly used
3. **Components**: Create in `$lib/components/{feature}/` with barrel `index.ts`
4. **State**: If needed, create `$lib/state/{feature}.svelte.ts`
5. **Route**: Create page in `routes/(app)/{feature}/`
6. **Server load**: Add `+page.server.ts` for initial data
7. **i18n**: Add keys to both `en.json` and `cs.json`
8. **Navigation**: Update sidebar/header if adding a new page
9. **Responsive**: Verify at 320px, 375px, 768px, 1024px
10. **Accessibility**: Touch targets ≥40px, logical properties, `prefers-reduced-motion`
11. **Docs**: Update `docs/` and AGENTS.md if the feature introduces new patterns or conventions (see root `AGENTS.md` — Documentation Maintenance)

Commit atomically: types+aliases → components → route+server-load → i18n keys.
