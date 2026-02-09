# Frontend Conventions (SvelteKit / Svelte 5)

This document explains the patterns, conventions, and design decisions used in the frontend. For quick-reference rules and code templates, see [`src/frontend/AGENTS.md`](../src/frontend/AGENTS.md).

## Svelte 5 — Runes Only

The entire frontend uses Svelte 5 Runes. The legacy `export let` syntax is not used anywhere and must not be introduced.

**Why Runes:** Runes (`$state`, `$props`, `$derived`, `$effect`) provide explicit, opt-in reactivity. Unlike Svelte 4's implicit reactivity (where `let` declarations were magically reactive in `.svelte` files), Runes make reactive declarations visible and consistent between `.svelte` and `.svelte.ts` files.

### Props — Always `interface Props`

Components declare props using a separate `interface Props` and destructure from `$props()`:

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

**Why not `$props<{ ... }>()`:** The generic syntax inlines the type, making it harder to read for components with more than 2-3 props. A named interface is self-documenting and can be exported if needed.

---

## Component Organization

### Feature Folders with Barrel Exports

Components live in feature folders under `$lib/components/`. Every folder has an `index.ts` that re-exports all components.

**Why feature folders over flat structure:** Components that work together (e.g., `ProfileForm`, `ProfileHeader`, `AvatarDialog`) should live together. A flat `components/` directory with 30+ files makes it hard to find related components.

**Why barrel exports:** Clean imports (`import { ProfileForm } from '$lib/components/profile'`) and a single place to see what a feature folder provides. Direct file imports (`import ProfileForm from '...ProfileForm.svelte'`) are prohibited — they bypass the barrel and create import inconsistency.

### shadcn-svelte Components

UI primitives (button, dialog, card, etc.) come from shadcn-svelte, installed via CLI (`npx shadcn-svelte@next add <name>`). These live in `$lib/components/ui/`.

**Key point:** shadcn components are **source code you own**, not a library. They are customizable and expected to be modified. When touching any shadcn component, convert physical CSS properties to logical (RTL support).

Browse the full catalog at [ui.shadcn.com](https://ui.shadcn.com) before building custom components — shadcn likely already has what you need.

---

## State Management

### Reactive State Files

State files use the `.svelte.ts` extension and live in `$lib/state/`. The `.svelte.ts` extension enables Svelte's reactivity system outside of components.

| File | Purpose |
|---|---|
| `shake.svelte.ts` | Field-level shake animations for validation errors |
| `theme.svelte.ts` | Light/dark/system theme management |
| `sidebar.svelte.ts` | Sidebar open/collapsed state |
| `shortcuts.svelte.ts` | Keyboard shortcut registration and display |

**Rule:** Never mix reactive state (`.svelte.ts`) with pure utility functions (`.ts`). If a module uses `$state`, `$derived`, or `$effect`, it must be `.svelte.ts`. If it's pure functions, it must be `.ts`.

**Why no global store:** The app's state needs are simple — theme, sidebar, keyboard shortcuts. A full store solution (Svelte stores, Zustand, etc.) would be overengineering. `.svelte.ts` files with exported reactive values are sufficient. If state complexity grows significantly, revisit this decision.

---

## API Client

### Two Client Variants

| Client | Where | When |
|---|---|---|
| Server client (`createApiClient(fetch, url.origin)`) | `+page.server.ts`, `+layout.server.ts` | Initial page data, SEO content |
| Browser client (`browserClient`) | Components, event handlers | User interactions, form submissions |

**Why two clients:** The server client uses SvelteKit's `fetch` (which forwards cookies on the server side and has access to internal network). The browser client makes requests from the browser. Both go through the same SvelteKit proxy, but they're created differently because SvelteKit's server-side `fetch` has special behavior (cookie forwarding, relative URL resolution).

### 401 Refresh Logic

When any request returns 401:
1. Trigger `POST /api/auth/refresh` (once, shared across concurrent requests)
2. If refresh succeeds, retry the original request with new cookies
3. If refresh fails, return 401 to the caller

Concurrent 401s share a single refresh promise — this prevents thundering herd on token expiry.

### Missing API Endpoints

If the backend doesn't provide data you need, **don't work around it**. Since we control the full stack, propose the backend endpoint first. This prevents building frontend hacks that accumulate tech debt.

---

## Styling

### CSS Architecture

Styles are modular in `src/styles/`. The entry point is `index.css`, which imports modules in order:

| File | Purpose |
|---|---|
| `themes.css` | HSL color tokens (`:root` + `.dark`) |
| `tailwind.css` | `@theme inline` mappings to CSS vars |
| `base.css` | `@layer base` element styles |
| `animations.css` | Keyframes + animation classes |
| `utilities.css` | Reusable effect classes (glow, card hovers) |

Tailwind CSS 4 is configured via the Vite plugin — there is no `tailwind.config.js` or `postcss.config.js`.

### Logical Properties — RTL Support

**All CSS uses logical properties.** Physical directional properties (`ml-`, `mr-`, `left-`, `right-`) break right-to-left layouts. Even though the app currently only supports LTR languages (English, Czech), using logical properties from the start means RTL support is free when needed.

| Physical | Logical |
|---|---|
| `ml-*` / `mr-*` | `ms-*` / `me-*` |
| `pl-*` / `pr-*` | `ps-*` / `pe-*` |
| `left-*` / `right-*` | `start-*` / `end-*` |
| `text-left` / `text-right` | `text-start` / `text-end` |
| `border-l` / `border-r` | `border-s` / `border-e` |

**Exception:** Animation classes from `tailwindcss-animate` (like `slide-in-from-left`) are animation names, not physical properties — they're acceptable.

### Responsive Design — Mobile First

Start with the smallest viewport (320px) and add breakpoints for larger screens. This is non-negotiable.

**Key rules and their rationale:**

- **Minimum font size 12px (`text-xs`)** — smaller text fails WCAG readability standards and is genuinely unreadable on mobile.
- **Touch targets minimum 40px (`h-10`)** — Apple's HIG and Google's Material Design both specify 44px minimum. 40px is our floor for secondary actions.
- **`h-dvh` not `h-screen`** — `h-dvh` (dynamic viewport height) accounts for mobile browser chrome. `h-screen` causes content to be hidden behind the address bar on mobile.
- **No flat large padding** — `p-16` on a 320px screen leaves almost no content space. Scale responsively: `p-4 sm:p-6 lg:p-8`.

### Theme Variables

The design system uses HSL color tokens defined in `themes.css` and mapped to Tailwind utilities in `tailwind.css`:

```css
/* 1. Define token */
:root { --accent: 210 40% 50%; }
.dark { --accent: 210 40% 60%; }

/* 2. Map to Tailwind */
@theme inline { --color-accent: hsl(var(--accent)); }

/* 3. Use in components */
<div class="bg-accent text-accent-foreground">
```

Dark mode uses a class-based strategy: the `.dark` class on `<html>` activates dark theme tokens. Theme persistence uses `localStorage` with a FOUC prevention script in `app.html` that applies the theme before Svelte hydration.

---

## Internationalization (i18n)

### Technology: Paraglide-JS

Paraglide provides type-safe, compile-time i18n. Translation keys are validated at build time — a missing key is a TypeScript error, not a runtime fallback to a key string.

### Key Naming Convention

```
{domain}_{feature}_{element}
```

Examples: `auth_login_title`, `profile_personalInfo_firstName`, `nav_dashboard`

### Important: Module Resolution

Paraglide generates `$lib/paraglide/*` modules at build time. Running `svelte-check` will show ~32 errors about these modules not being found. These are **not real errors** — the modules exist at runtime and after a full build. Don't try to fix them.

### Adding Translations

Edit both `src/messages/en.json` and `src/messages/cs.json`. If you add a key to one file, add it to the other. The build will fail if a key exists in the base locale (English) but not in a secondary locale.

---

## Route Structure

### Route Groups

| Group | Purpose | Guard |
|---|---|---|
| `(app)` | All authenticated pages | Redirect to `/login` if no user |
| `(public)` | Login page | Redirect to `/` if already authenticated |
| `api` | API proxy to backend | CSRF origin validation |

### Data Fetching Strategy

| Need | Pattern | Why |
|---|---|---|
| Initial page data | `+page.server.ts` with server client | Data available on first render, SEO-friendly |
| User-triggered updates | `browserClient` in component | Responsive to user actions, no page reload |
| Refresh after mutation | `browserClient` → update `$state` | Optimistic or server-confirmed updates |

The root layout fetches the user once. Child layouts access it via `parent()`. Pages that need additional data use their own `+page.server.ts`.

### Role-Based Access

Role checks happen at two levels:

1. **Backend (authoritative)** — `[Authorize(Roles = "...")]` rejects unauthorized API calls with 403.
2. **Frontend (UX)** — Layout guards and conditional rendering prevent users from seeing UI they can't use.

The backend is always the source of truth. Frontend role checks are convenience, not security.

---

## Error Handling

### Validation Errors

ASP.NET Core returns `ValidationProblemDetails` with field-level errors. The frontend:
1. Detects validation errors with `isValidationProblemDetails()`
2. Maps PascalCase field names to camelCase with `mapFieldErrors()`
3. Triggers shake animations on errored fields with `createFieldShakes()`

### Generic Errors

Non-validation errors are extracted with `getErrorMessage()` and displayed as toast notifications.

### Network Errors

The API proxy handles `ECONNREFUSED` by returning 503 "Backend unavailable". This gives the user a clear message instead of a cryptic network error.

---

## TypeScript Patterns

### Strict Mode

TypeScript is configured in strict mode. `any` is prohibited — define proper interfaces for all data shapes.

### Type Narrowing Over Assertions

Prefer `if ('field' in obj)` narrowing over `as` casts. Narrowing is checked by the compiler; assertions are not.

### localStorage

Always wrap `localStorage` access in `try/catch` — it throws in private browsing mode and when storage quota is exceeded.

### Navigation

Always use `resolve()` from `$app/paths` with `goto()` for base-path-aware navigation. The `svelte/no-navigation-without-resolve` lint rule enforces this.
