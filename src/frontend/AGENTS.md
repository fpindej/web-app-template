# Frontend Conventions (SvelteKit / Svelte 5)

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

When a page needs multiple independent API calls, use `Promise.all` for parallel fetching:

```typescript
export const load: PageServerLoad = async ({ fetch, url, params }) => {
	const client = createApiClient(fetch, url.origin);
	const [userResult, rolesResult] = await Promise.all([
		client.GET('/api/v1/admin/users/{id}', { params: { path: { id: params.id } } }),
		client.GET('/api/v1/admin/roles')
	]);
	return { user: userResult.data, roles: rolesResult.data };
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

### Error Handling (Generic Errors)

The backend returns a descriptive `message` on every error response. The frontend uses this message directly via `getErrorMessage()`:

```typescript
import { getErrorMessage, browserClient } from '$lib/api';
import { toast } from '$lib/components/ui/sonner';
import * as m from '$lib/paraglide/messages';

const { response, error: apiError } = await browserClient.POST('/api/auth/register', { body });

if (!response.ok) {
	toast.error(getErrorMessage(apiError, m.auth_register_error()));
}
```

#### `getErrorMessage()` Resolution Order

1. **`message` field** → backend's descriptive error message (ErrorResponse shape)
2. **`detail` field** → ProblemDetails detail
3. **`title` field** → ProblemDetails title
4. **Fallback string** → the caller-provided fallback

The backend always returns specific, user-friendly English messages with every error (e.g., `"Username 'user@example.com' is already taken."`). The frontend displays these messages directly — no translation or mapping is needed.

### Validation Errors (Field-Level)

ASP.NET Core returns `ValidationProblemDetails` with field-level errors. Handle them with the provided utilities:

```typescript
import {
	isValidationProblemDetails,
	mapFieldErrors,
	getErrorMessage,
	browserClient
} from '$lib/api';
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

### Content Security Policy (CSP)

CSP is configured via nonce mode in `svelte.config.js` using SvelteKit's built-in `kit.csp`:

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

Key decisions:

- **`script-src`**: Nonce-based. The FOUC prevention script in `app.html` uses `%sveltekit.nonce%`.
- **`style-src`**: Requires `'unsafe-inline'` because Svelte transitions (`fly`, `scale`) inject inline `<style>` elements at runtime. This is a documented SvelteKit limitation.
- **`img-src`**: `'self' https: data:` allows external avatar URLs over HTTPS and Vite-inlined assets. Vite inlines files under 4KB as `data:` URIs at build time — this affects the favicon SVG and most `flag-icons` CSS sprites (country flags used in the phone input and language selector). Without `data:`, these assets are blocked by CSP.
- **`frame-ancestors`**: `'none'` — defense-in-depth alongside `X-Frame-Options: DENY`.

CSP is set by the SvelteKit framework (added to responses automatically). The `hooks.server.ts` does NOT set CSP — there is no conflict.

### HSTS

Strict-Transport-Security is added in `hooks.server.ts` with a **production-only guard**:

```typescript
if (!dev) {
	response.headers.set('Strict-Transport-Security', 'max-age=63072000; includeSubDomains');
}
```

Never enable HSTS in development — it breaks `localhost` HTTP.

### CSRF Protection

The API proxy at `routes/api/[...path]/+server.ts` validates the `Origin` header on state-changing requests (POST/PUT/PATCH/DELETE). Requests whose origin doesn't match are rejected with 403. This complements SvelteKit's built-in CSRF protection, which only covers form actions — not `+server.ts` routes.

The check allows:

1. **Same-origin requests** — `Origin` matches `url.origin` (the SvelteKit server's own origin)
2. **Configured origins** — `Origin` matches an entry in `ALLOWED_ORIGINS` (env var, comma-separated)
3. **Missing `Origin` header** — safe to allow (same-origin older browsers or non-browser clients)

To allow access through a reverse proxy or tunnel (ngrok, Tailscale), set the `ALLOWED_ORIGINS` environment variable:

```bash
# In .env
ALLOWED_ORIGINS=https://abc123.ngrok-free.app
```

Multiple origins are comma-separated: `ALLOWED_ORIGINS=https://a.example.com,https://b.example.com`

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

### Reactive Collections

Svelte 5 provides reactive collection classes in `svelte/reactivity`. Use these instead of plain `Set`/`Map`/`URLSearchParams` when reactivity is needed:

```typescript
import { SvelteSet } from 'svelte/reactivity';
import { SvelteURLSearchParams } from 'svelte/reactivity';

const activeFields = new SvelteSet<string>();   // Reactive Set — auto-tracks adds/deletes
const params = new SvelteURLSearchParams(url.searchParams); // Reactive query string manipulation
```

These are used in the codebase for shake state management (`SvelteSet`) and admin pagination/search (`SvelteURLSearchParams`).

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
├── admin/           # UserTable, Pagination, RoleTable, UserDetailCards,
│   └── index.ts     # UserManagementCard, AccountInfoCard
├── auth/            # LoginForm, LoginBackground, RegisterDialog
│   └── index.ts     # Barrel export
├── getting-started/ # GettingStarted, markdown.ts (removable starter page)
│   └── index.ts
├── layout/          # Header, Sidebar, SidebarNav, UserNav,
│   └── index.ts     # ThemeToggle, LanguageSelector, ShortcutsHelp
├── profile/         # ProfileForm, ProfileHeader, AvatarDialog,
│   └── index.ts     # AccountDetails, InfoItem
├── settings/        # ChangePasswordForm, DeleteAccountDialog
│   └── index.ts
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

### Paraglide Module Resolution

Paraglide generates `$lib/paraglide/*` modules at build time. When running `svelte-check`, you will see ~32 errors like:

```
Cannot find module '$lib/paraglide/messages' or its corresponding type declarations
```

These are **not real errors** — the modules exist at runtime. The errors disappear after a full build (`npm run build`). Do not try to fix them.

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

Tailwind CSS 4 is configured via the Vite plugin (`@tailwindcss/vite`) — there is no `tailwind.config.js` or `postcss.config.js`. The `tailwindcss-animate` plugin is loaded via `@plugin 'tailwindcss-animate'` in `index.css`.

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

**Note on `space-x-*`:** This uses `margin-left` internally (physical). On flex/grid containers, prefer `gap-*` which is direction-agnostic. Only use `space-x-*` when not in a flex/grid context.

**Note on animation classes:** Classes like `slide-in-from-left`, `slide-out-to-right` from `tailwindcss-animate` are animation names, not physical properties — they are acceptable.

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

10. **Account for sidebar width when choosing grid breakpoints.** The app sidebar consumes ~200–250px on `md:` and above. A `lg:grid-cols-2` (1024px) leaves only ~770px for content — too cramped for two card columns. Use `xl:grid-cols-2` (1280px) for content-area grids so tablets (iPad Pro at 1024px) stay single-column.

    ```html
    <!-- ✅ Correct — 2-col kicks in at 1280px, comfortable with sidebar -->
    <div class="grid gap-6 xl:grid-cols-2">
    	<!-- ❌ Wrong — 2-col at 1024px is cramped when sidebar takes ~250px -->
    	<div class="grid gap-6 lg:grid-cols-2"></div>
    </div>
    ```

11. **Don't constrain card widths with `max-w-*`.** Cards inside the app layout should fill their container. The sidebar + main area padding already constrains width. Adding `max-w-2xl` or similar creates an awkward gap on the right side of wider screens.

#### Page Layout Patterns

| Page type                  | Layout                             | Example                                                                                     |
| -------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------------- |
| **Info + actions** (2-col) | `grid gap-6 xl:grid-cols-2`        | Profile (form left, account details right), Admin user detail (info left, management right) |
| **Single-column forms**    | `space-y-8` (no max-width)         | Settings (change password, danger zone stacked vertically)                                  |
| **Table + search**         | Full-width table, search bar above | Admin users list, roles list                                                                |

Key principles:

- **`xl:grid-cols-2`** (not `lg:`) for side-by-side layouts — accounts for sidebar width on tablets
- **No `max-w-*`** on page content — the app shell already constrains width via sidebar + padding
- **Cards fill their container** — they provide their own visual boundaries

#### Test Viewports

When making responsive changes, mentally verify at these widths: **320px**, **375px**, **768px**, **1024px**, **1440px**.

#### Existing Responsive Patterns (Reference)

The app layout uses a mobile-first sidebar pattern:

- `md:hidden` — mobile hamburger menu (sheet drawer)
- `hidden md:block` — desktop sidebar
- `h-dvh` — full dynamic viewport height
- Content grids: `xl:grid-cols-2` (sidebar-aware — not `lg:`)
- Feature grids: `sm:grid-cols-2 xl:grid-cols-4`
- Dialog footers: `flex-col-reverse sm:flex-row`
- Text sizes: `text-sm sm:text-base md:text-lg`

## Route Structure & Data Fetching

### Authentication Flow

```
Browser request
    │
    ▼
hooks.server.ts          ← Security headers, locale detection
    │
    ▼
+layout.server.ts (root) ← Calls getUser() → GET /api/users/me
    │                        Returns { user, locale, apiUrl }
    │
    ├─► (app)/+layout.server.ts    ← user is null? → redirect 303 /login
    │                                  user exists?  → pass through { user }
    │
    └─► (public)/login/+page.server.ts ← user exists? → redirect 303 /
                                          user is null? → show login page
```

The root layout fetches the user **once** via `getUser()`. All child layouts access the user through `parent()` — they never re-fetch.

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

To add a route group with additional access requirements (e.g., admin-only pages):

1. **Create the route group directory** with its own layout server load:

   ```
   routes/
   ├── (app)/           # Authenticated users
   ├── (admin)/         # Admin-only (new)
   │   ├── +layout.server.ts
   │   ├── +layout.svelte
   │   └── admin-panel/
   └── (public)/        # Unauthenticated
   ```

2. **Add a layout guard** that checks both authentication and roles:

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

3. **Keep authorization on the backend too.** Frontend guards are UX — they prevent users from seeing pages they can't use. The backend `[Authorize(Roles = "Admin")]` is the actual security boundary. Never rely on frontend-only role checks.

### Role-Based Access

**Current state:** The frontend has no role-based routing. Roles are only displayed as badges on the profile page (`AccountDetails.svelte`). The `UserResponse` from the backend includes `roles: string[]`.

**Architecture:** Role-based access is enforced at two levels:

| Level                       | Mechanism                                             | Purpose                                                        |
| --------------------------- | ----------------------------------------------------- | -------------------------------------------------------------- |
| **Backend** (authoritative) | `[Authorize(Roles = "...")]` on controllers/endpoints | Security enforcement — rejects unauthorized API calls with 403 |
| **Frontend** (UX)           | Layout guards + conditional rendering                 | Prevents users from seeing UI they can't use                   |

The backend is always the source of truth. If a user manipulates the frontend to bypass a role check, the API call will still fail with 403.

#### Conditional Rendering by Role

To show/hide UI elements based on the current user's roles:

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

#### Future: Granular Permissions

The current model is role-based (`roles: string[]`). If granular permissions are added later (e.g., `permissions: string[]` on the user response), the same patterns apply:

- Backend enforces permissions via policy-based authorization
- Frontend checks `user.permissions?.includes('orders:write')` for conditional rendering
- Layout guards check permissions for route-level access
- The backend remains the single source of truth

Do **not** pre-build a permissions system. When the backend adds permissions to the user response, extend the frontend patterns above.

### Root Layout Data

The root `+layout.server.ts` fetches the user and locale for all routes. In development, it also exposes the internal API URL for debugging (visible in the login page status indicator):

```typescript
export const load: LayoutServerLoad = async ({ locals, fetch, url }) => {
	const user = await getUser(fetch, url.origin);
	return { user, locale: locals.locale, apiUrl: dev ? SERVER_CONFIG.API_URL : undefined };
};
```

### Universal Load Function

The root `+layout.ts` (universal) sets the paraglide locale from server data so that i18n works on both server and client:

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

`app.html` contains an inline `<script>` that reads the theme from `localStorage` and applies the `.dark` class before paint, preventing a flash of unstyled content. This script runs before Svelte hydration.

### Raw File Imports

Use Vite's `?raw` suffix to import file contents as strings (used in GettingStarted for README display):

```typescript
import readmeContent from '../../../../README.md?raw';
```

This is a Vite feature — the file content is bundled as a string at build time.

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

`svelte-check` will report ~32 errors related to `$lib/paraglide/*` module resolution. These are expected — paraglide modules are generated at build time. Only investigate errors that are NOT about paraglide imports.

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
- `lg:grid-cols-2` for content-area grids — use `xl:grid-cols-2` (sidebar takes ~250px at `lg:`)
- `max-w-2xl` or similar on page content — cards should fill their container
- `null!` non-null assertions
- Import server config from barrel (`$lib/config`)
- Leave components in `$lib/components/` root — use feature folders
- Mix reactive state (`.svelte.ts`) with pure utils (`.ts`)
- Hand-edit `v1.d.ts` — run `npm run api:generate`
- Create UI components that shadcn already provides
- Work around missing API endpoints — propose them instead
- Suppress `svelte/no-navigation-without-resolve` — use `resolve()` with `goto()`
- Silent failures — always handle errors explicitly

## Testing

No test infrastructure is currently set up — no vitest, playwright, or other test frameworks are configured. When tests are added, this section will document the testing frameworks, patterns, and conventions.

## Adding a New Feature

For step-by-step procedures (add page, add component, add feature), see [`SKILLS.md`](../../SKILLS.md). This file documents **conventions and patterns** — SKILLS.md documents **workflows and checklists**.
