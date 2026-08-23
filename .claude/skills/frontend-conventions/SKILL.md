---
description: "Frontend convention reference (SvelteKit / Svelte 5). Auto-injected into frontend-aware agents - not user-invocable."
user-invocable: false
---

# Frontend Conventions (SvelteKit / Svelte 5)

## Project Structure

```
src/
├── lib/
│   ├── api/                       # client.ts, error-handling.ts, mutation.ts, backend-monitor.ts, v1.d.ts (GENERATED)
│   ├── auth/                      # auth.ts (getUser, logout), middleware.ts (token refresh)
│   ├── components/
│   │   ├── ui/                    # shadcn (generated, customizable)
│   │   ├── auth/                  # Login/register/reset flows, 2FA step, Turnstile
│   │   ├── layout/                # Header, sidebar, user nav, theme/language toggles
│   │   ├── profile/               # Profile forms, avatar, account details
│   │   ├── settings/              # Password, account deletion, activity log, 2FA management
│   │   ├── admin/                 # User/role/job management, audit trail
│   │   ├── dashboard/             # Dashboard widgets
│   │   ├── oauth/                 # OAuth provider linking
│   │   └── common/                # Small shared pieces (status indicator, etc.)
│   │   # For the live component inventory, list the barrels: $lib/components/*/index.ts
│   ├── config/                    # i18n.ts (client-safe), server.ts (server-only - never export from barrel)
│   ├── state/                     # .svelte.ts files only (cooldown, health, shake, theme, sidebar, shortcuts)
│   ├── types/index.ts             # Type aliases from API schemas
│   └── utils/                     # ui.ts (cn()), permissions.ts, audit.ts, platform.ts, roles.ts, jobs.ts
├── routes/
│   ├── (app)/                     # Authenticated (redirect guard)
│   │   └── admin/                 # Permission-guarded per page
│   ├── (public)/login/            # Redirect away if logged in
│   └── api/[...path]/             # Catch-all proxy to backend
├── messages/{locale}/*.json        # i18n (per-feature: core, auth, admin, etc.)
└── styles/                        # themes.css, tailwind.css, animations.css, base.css, utilities.css
```

## API Client

Two layers: `lib/api/` (auth-agnostic client factory) and `lib/auth/` (all auth concerns).

| Export | Module | Purpose |
|---|---|---|
| `createApiClient(fetch?, baseUrl?, middleware?)` | `$lib/api` | Creates typed openapi-fetch client. Server load functions pass `fetch` + `url.origin`. |
| `browserClient` | `$lib/api` | Singleton for client-side code. Created bare - auth wired at runtime. |
| `initBrowserAuth(middleware)` | `$lib/api` | Registers auth middleware on `browserClient` exactly once (idempotent guard). |
| `createAuthMiddleware(fetch, baseUrl, onAuthFailure?)` | `$lib/auth` | 401 -> deduplicated refresh -> retry idempotent methods only. |
| `getUser(fetch, origin)` | `$lib/auth` | Returns `GetUserResult` - distinguishes "not authenticated" from "backend unavailable". |

**Auth middleware flow**: 401 -> deduplicated refresh -> retry GET/HEAD/OPTIONS only. Non-idempotent methods return 401 to caller (prevents double-submission).

### Type Generation

Type aliases in `$lib/types/index.ts`:

```typescript
import type { components } from '$lib/api/v1';
export type User = components['schemas']['UserResponse'];
```

If the backend doesn't provide data you need - propose the endpoint, don't work around it.

**File uploads**: Use native `fetch()` with `FormData` - not `browserClient`. The openapi-fetch typed client doesn't reliably handle multipart/`File` objects. After upload success, call `invalidateAll()` to refresh server data.

## Error Handling

### Generic Errors

```typescript
import { getErrorMessage, browserClient } from '$lib/api';
const { response, error } = await browserClient.POST('/api/...', { body });
if (!response.ok) toast.error(getErrorMessage(error, m.fallback_message()));
```

### Translating Specific Errors

Every backend `ProblemDetails` carries a stable snake_case `code` (see `ErrorMessages.cs`) next to the English `detail`. Branch on `code` - never on `detail` text. Pass an `ErrorMessagesByCode` map to translate known codes; unknown codes fall back to `detail`, then `title`, then the fallback:

```typescript
import { getErrorMessage } from '$lib/api';
const message = getErrorMessage(error, m.auth_login_error(), {
	auth_login_invalid_credentials: m.auth_login_invalidCredentials,
	auth_login_account_locked: m.auth_login_accountLocked
});
```

### Mutations (Validation + Rate Limiting)

```typescript
import { browserClient, handleMutationError } from '$lib/api';
import { createCooldown, createFieldShakes } from '$lib/state';

const cooldown = createCooldown();
const fieldShakes = createFieldShakes();
let fieldErrors = $state<Record<string, string>>({});

const { response, error } = await browserClient.PATCH('/api/...', { body });
if (response.ok) {
	toast.success(m.success());
} else {
	handleMutationError(response, error, {
		cooldown,
		fallback: m.error(),
		onValidationError(errors) {
			fieldErrors = errors;
			fieldShakes.triggerFields(Object.keys(errors));
		}
	});
}
```

**Rate-limited buttons should show countdown:**

```svelte
<Button disabled={isLoading || cooldown.active}>
	{#if cooldown.active}{m.common_waitSeconds({ seconds: cooldown.remaining })}
	{:else if isLoading}<Loader2 class="me-2 h-4 w-4 animate-spin" />{m.submit()}
	{:else}{m.submit()}{/if}
</Button>
```

## Component Rules

### Props

Use `interface Props` + destructure from `$props()`:

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

### Organization

Import via barrel only:

```typescript
import { ProfileForm, AvatarDialog } from '$lib/components/profile';
```

### shadcn

Add via CLI: `pnpm dlx shadcn-svelte@latest add <name>`. Check [ui.shadcn.com](https://ui.shadcn.com) before building custom UI. Convert physical CSS to logical in generated components.

## Styling

### Logical Properties Only

Use `ms-*`/`me-*`/`ps-*`/`pe-*`/`start-*`/`end-*`/`text-start`/`text-end`/`border-s`/`border-e`/`gap-*` instead of physical equivalents (`ml-*`/`mr-*`/`pl-*`/`pr-*`/`left-*`/`right-*`/`text-left`/`text-right`/`border-l`/`border-r`/`space-x-*`). Full mapping in the [design tokens reference](../review-design/references/design-tokens.md).

### Button Layout (Action/Submit Buttons)

- **Mobile**: `w-full` (full width, stacked vertically)
- **Desktop**: `sm:w-auto` (auto width, right-aligned)
- **Wrapper**: `flex flex-col gap-2 sm:flex-row sm:justify-end`
- **Size**: Default size everywhere - no `size="sm"` or `size="lg"` overrides on action buttons

### Responsive Design (Mobile-First)

- Base styles for 320px, then `sm:` -> `md:` -> `lg:` -> `xl:`
- Touch targets minimum 44px (`min-h-11`) on all interactive elements (CLAUDE.md hard rule)
- `h-dvh` not `h-screen` for full-height layouts
- `min-w-0` on flex children with text, `shrink-0` on icons/badges
- **Content grids: `lg:grid-cols-2`** - the `max-w-7xl` constraint ensures sufficient column width even with the sidebar
- **Page content max-width: `max-w-7xl mx-auto`** - prevents ultra-wide layouts
- Scale padding with breakpoints (`p-4 sm:p-6 lg:p-8`)
- Dialog grids: start with `grid-cols-1` base
- Min font: `text-xs` (12px)
- Animations: always `motion-safe:` prefix

### Theming

CSS variables in `themes.css` (`:root` + `.dark`), mapped in `tailwind.css` (`@theme inline`). Use `cn()` from `$lib/utils` for class merging.

## Routing & Auth

```
hooks.server.ts -> +layout.server.ts (root: getUser) -> (app)/+layout.server.ts (503 if backend down, redirect if no user)
                                                       -> (public)/+layout.server.ts (503 if backend down)
                                                           -> login/+page.server.ts (redirect if user exists)
```

Root layout fetches user **once** via `getUser()` which returns `GetUserResult` - distinguishes "not authenticated" from "backend unavailable".

### Permission Guards

1. **`adminRoutes` registry** (`$lib/config/routes.ts`): single source of truth pairing each admin path with its required permission
2. **Admin layout**: broad gate - derives permission list from `Object.values(adminRoutes)`
3. **Individual pages**: specific permission check via `adminRoutes.X.permission` -> redirect to `routes.dashboard`
4. **Sidebar/CommandPalette**: filter items per-permission using `adminRoutes.X.permission`
5. **Backend is authoritative** - frontend guards are UX only

```typescript
// Route guards (server-side)
import { adminRoutes, routes } from '$lib/config';
if (!hasPermission(user, adminRoutes.users.permission)) throw redirect(303, routes.dashboard);

// UI permission checks (client-side)
import { hasPermission, Permissions } from '$lib/utils';
let canManage = $derived(hasPermission(data.user, Permissions.Users.Manage));
```

### Graceful Degradation for Secondary Fetches

Primary entity failure throws (hard error), but secondary data failures return empty arrays with a `*LoadFailed` flag. Components must consume the flag and show an `Alert` warning.

## i18n

Keys: `{domain}_{feature}_{element}` (e.g., `auth_login_title`, `profile_personalInfo_firstName`).

Add to the correct feature file in all locale directories (e.g., `messages/en/auth.json`). Files: `core`, `dashboard`, `auth`, `admin`, `jobs`, `audit`, `2fa`, `oauth`, `avatars`. Locales are configured in `project.inlang/settings.json`. Use: `import * as m from '$lib/paraglide/messages'; m.key_name()`.

`svelte-check` reports ~32 paraglide module errors - these are expected (generated at build time). Ignore them.

## State

`.svelte.ts` files in `$lib/state/` only. Keep reactive state separate from pure utility functions.

| File | Exports |
|---|---|
| `cooldown.svelte.ts` | `createCooldown()` - rate-limit countdown |
| `shake.svelte.ts` | `createShake()`, `createFieldShakes()` |
| `theme.svelte.ts` | `getTheme()`, `setTheme()`, `toggleTheme()` |
| `health.svelte.ts` | `healthState`, `initHealthCheck()` - adaptive backend polling |
| `shortcuts.svelte.ts` | `shortcutsState`, `globalShortcuts()` action, `getAllShortcuts()`, `getShortcutSymbol()` |

## File Upload

Use native `fetch()` with `FormData` for file uploads.

**Avatar URLs:** If `user.hasAvatar` is true, construct `/api/users/${user.id}/avatar?v=${version}` where `version` is a `$state` variable bumped on avatar dialog close.

## Security

### Response Headers (hooks.server.ts)

`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: camera=(), microphone=(), geolocation=()`. HSTS in production only.

### CSP (svelte.config.js)

Nonce-based `script-src`. `style-src: unsafe-inline` required for Svelte transitions. `img-src: self https: data: blob:`.

### CSRF

API proxy validates `Origin` header on mutations. Same-origin + `ALLOWED_ORIGINS` env var allowed.

## Testing

Co-locate tests: `foo.ts` -> `foo.test.ts`. Use vitest with explicit `import { describe, it, expect, vi } from 'vitest'`. Default environment is `node`; add `// @vitest-environment jsdom` for DOM tests. `restoreMocks: true` handles cleanup globally. See the `/add-test` skill for full setup, mock patterns, and test helpers.

```bash
pnpm run test              # all tests (CI mode)
pnpm run test:watch        # watch mode
```

## TypeScript Strictness

- **`noUncheckedIndexedAccess: true`** - array/object index access returns `T | undefined`. Guard before using.
- **`@typescript-eslint/no-explicit-any: 'error'`** - `any` is a lint error. Use `unknown`, generics, or proper interfaces.

## Anti-Patterns to Avoid

- `h-screen` - use `h-dvh`
- `xl:grid-cols-2` for content - use `lg:grid-cols-2`
- Unconstrained page content - always use `max-w-7xl mx-auto` wrapper
- `size="sm"` or `size="lg"` on action/submit buttons - use default size with `w-full sm:w-auto`
- Left-aligned action buttons - always right-align with `sm:justify-end` wrapper
- `null!`, `as` casts when narrowing works
- Import server config from barrel (`$lib/config`)
- Hand-edit `v1.d.ts`
- Components in `$lib/components/` root - use feature folders
- Mix `.svelte.ts` (reactive) with `.ts` (pure)
- Build what shadcn already provides
