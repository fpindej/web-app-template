# GitHub Copilot Instructions

You are an expert SvelteKit developer working on a production-grade application. Maintain S-Tier architecture: scalable, maintainable, and type-safe.

---

## Tech Stack

| Layer     | Technology                     | Notes                                     |
| --------- | ------------------------------ | ----------------------------------------- |
| Framework | SvelteKit + **Svelte 5 Runes** | `$state`, `$props`, `$effect`, `$derived` |
| Language  | TypeScript (Strict)            | No `any`, define interfaces               |
| Styling   | Tailwind CSS 4                 | CSS variables in `src/styles/`            |
| UI        | shadcn-svelte (`bits-ui`)      | Headless, accessible components           |
| i18n      | `paraglide-js`                 | Type-safe `m.domain_feature_key()`        |
| API       | `openapi-fetch`                | Type-safe client from OpenAPI spec        |
| Backend   | ASP.NET Core                   | ProblemDetails error format               |

---

## API Type Generation

The API client types are auto-generated from the backend's OpenAPI specification.

### Regenerating Types

Whenever the backend API changes, regenerate the types:

```bash
npm run api:generate
```

This fetches the OpenAPI spec from the backend and generates `src/lib/api/v1.d.ts`.

### Prerequisites

- Backend must be running (serves OpenAPI spec at `/swagger/v1/swagger.json`)
- Check `package.json` for the exact endpoint URL

### After Regenerating

1. Review changes in `v1.d.ts` for breaking changes
2. Update any affected API calls
3. Run `npm run check` to catch type errors

### Type Usage

Types are automatically available through the API client:

```typescript
import { browserClient } from '$lib/api';

// Response types are inferred automatically
const { data } = await browserClient.GET('/api/users/me');
// data is typed as UserResponse | undefined

// Request body types are enforced
await browserClient.PATCH('/api/users/me', {
	body: { firstName: 'John' } // TypeScript validates this
});
```

For explicit type imports:

```typescript
import type { components } from '$lib/api/v1';

type User = components['schemas']['UserResponse'];
type UpdateUserRequest = components['schemas']['UpdateUserRequest'];
```

### Missing API Endpoints or Data

If the backend doesn't provide an endpoint or data you need, **don't work around it**. Since we control the full stack:

1. **Ask first**: "The backend doesn't expose X. Should I request this feature from the backend team?"
2. **Propose the endpoint**: Describe what you need (HTTP method, path, request/response shape)
3. **Wait for confirmation** before implementing frontend workarounds

Example request:

> "The profile page needs to display the user's notification preferences, but `/api/users/me` doesn't include this data. Should I request a `GET /api/users/me/preferences` endpoint or extend the existing response?"

This ensures the API evolves properly rather than accumulating frontend hacks.

---

## Adding New UI Components

When you need a UI component that doesn't exist yet, check [shadcn-svelte](https://shadcn-svelte.com/) first:

```bash
# Generate a new shadcn component
npx shadcn-svelte@next add <component-name>

# Examples:
npx shadcn-svelte@next add tooltip
npx shadcn-svelte@next add tabs
npx shadcn-svelte@next add accordion
```

After generating:

1. Review the generated files in `src/lib/components/ui/<component>/`
2. Ensure styles match our conventions (logical properties, CSS variables)
3. Update the component if needed to follow Svelte 5 patterns
4. Test the component works correctly

**Do not** manually create UI components that shadcn already provides.

---

## Project Structure

```
src/
├── lib/
│   ├── api/                    # API client & error handling
│   │   ├── client.ts           # createApiClient(), browserClient
│   │   ├── error-handling.ts   # isValidationProblemDetails(), mapFieldErrors()
│   │   └── v1.d.ts             # Generated OpenAPI types
│   │
│   ├── auth/                   # Authentication feature
│   │   └── auth.ts             # getUser(), logout()
│   │
│   ├── config/                 # Configuration
│   │   ├── i18n.ts             # LANGUAGE_METADATA (client-safe)
│   │   ├── index.ts            # Client-safe exports only
│   │   └── server.ts           # SERVER_CONFIG (import directly, not from barrel)
│   │
│   ├── state/                  # Reactive state (.svelte.ts files)
│   │   ├── shake.svelte.ts     # createShake(), createFieldShakes()
│   │   ├── shortcuts.svelte.ts # Keyboard shortcuts
│   │   └── theme.svelte.ts     # getTheme(), setTheme(), toggleTheme()
│   │
│   ├── types/                  # Type aliases
│   │   └── index.ts            # User, etc.
│   │
│   ├── utils/                  # Pure utility functions
│   │   ├── platform.ts         # IS_MAC, IS_WINDOWS
│   │   └── ui.ts               # cn() for class merging
│   │
│   └── components/
│       ├── ui/                 # shadcn components (presentational only)
│       ├── auth/               # LoginForm, RegisterDialog
│       ├── layout/             # Header, Sidebar, UserNav
│       ├── profile/            # ProfileForm, AvatarDialog
│       └── common/             # Shared components
│
└── styles/                     # Global CSS (imported in +layout.svelte)
    ├── index.css               # Entry point - imports all modules
    ├── themes.css              # CSS variables for light/dark themes
    ├── tailwind.css            # Tailwind @theme inline configuration
    ├── base.css                # @layer base styles
    ├── animations.css          # Keyframes and animation utilities
    └── utilities.css           # Reusable effect classes
```

### Import Rules

```typescript
// ✅ Use barrel exports
import { Header, Sidebar } from '$lib/components/layout';
import { createShake } from '$lib/state';
import { isValidationProblemDetails, browserClient } from '$lib/api';
import { cn } from '$lib/utils';

// ❌ Never import directly from files
import Header from '$lib/components/layout/Header.svelte';

// ⚠️ Server config: import directly (not from barrel)
import { SERVER_CONFIG } from '$lib/config/server';
```

---

## Svelte 5 Patterns

### Component Props

```svelte
<script lang="ts">
	interface Props {
		user: User;
		onSave?: (data: FormData) => void;
		class?: string; // Allow className passthrough
	}

	let { user, onSave, class: className }: Props = $props();
</script>
```

### Reactive State

```svelte
<script lang="ts">
	// Local state
	let count = $state(0);
	let items = $state<string[]>([]);

	// Derived values
	let doubled = $derived(count * 2);
	let hasItems = $derived(items.length > 0);

	// Effects (side effects on state change)
	$effect(() => {
		console.log('Count changed:', count);
	});
</script>
```

### Bindable Props

```svelte
<script lang="ts">
	interface Props {
		open: boolean; // Two-way binding
	}

	let { open = $bindable() }: Props = $props();
</script>

<!-- Usage -->
<Dialog bind:open={isOpen} />
```

### Snippets (replacing slots)

```svelte
<!-- Child (Card.svelte) -->
<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		header?: Snippet;
		content?: Snippet;
	}

	let { header, content }: Props = $props();
</script>

<!-- Parent -->
<Card>
	{#snippet header()}
		<h2>Title</h2>
	{/snippet}
	{#snippet content()}
		<p>Body</p>
	{/snippet}
</Card>

<div class="card">
	{#if header}{@render header()}{/if}
	{#if content}{@render content()}{/if}
</div>
```

---

## API & Error Handling

### Making API Calls

```typescript
import { browserClient } from '$lib/api';

const { data, response, error } = await browserClient.GET('/api/users/me');

if (response.ok && data) {
	// Success
} else if (error) {
	// Handle error
}
```

### Handling Validation Errors (ASP.NET Core)

```typescript
import { isValidationProblemDetails, mapFieldErrors } from '$lib/api';

if (isValidationProblemDetails(apiError)) {
	// Maps { PhoneNumber: ["Invalid"] } → { phoneNumber: "Invalid" }
	fieldErrors = mapFieldErrors(apiError.errors);
	fieldShakes.triggerFields(Object.keys(fieldErrors));
} else {
	toast.error(apiError?.detail || 'An error occurred');
}
```

### Field-Level Shake Animation

```svelte
<script lang="ts">
	import { createFieldShakes } from '$lib/state';

	const fieldShakes = createFieldShakes();

	async function handleSubmit() {
		// On validation error:
		fieldShakes.trigger('phoneNumber');
		// Or multiple:
		fieldShakes.triggerFields(['email', 'password']);
	}
</script>

<Input class={fieldShakes.class('phoneNumber')} />
```

---

## Styling (Tailwind 4)

### CSS Architecture

Styles are organized in `src/styles/` for modularity and future extensibility (e.g., user theme preferences):

| File             | Purpose                                  | When to Edit                                 |
| ---------------- | ---------------------------------------- | -------------------------------------------- |
| `themes.css`     | CSS custom properties (`:root`, `.dark`) | Adding new color tokens                      |
| `tailwind.css`   | `@theme inline` mappings to CSS vars     | Extending Tailwind's design system           |
| `base.css`       | `@layer base` element styles             | Global element resets                        |
| `animations.css` | `@keyframes` and animation classes       | Adding new animations                        |
| `utilities.css`  | Reusable effect classes                  | Glow effects, card hovers, status indicators |
| `index.css`      | Entry point (imports all above)          | Rarely - only to add new modules             |

**Adding a new theme variable:**

```css
/* 1. Define in themes.css */
:root {
  --accent: 210 40% 50%;
}
.dark {
  --accent: 210 40% 60%;
}

/* 2. Map in tailwind.css */
@theme inline {
  --color-accent: hsl(var(--accent));
}

/* 3. Use in components */
<div class="bg-accent text-accent-foreground">
```

**Adding a new animation:**

```css
/* In animations.css */
@keyframes slide-in {
	from {
		transform: translateX(-100%);
	}
	to {
		transform: translateX(0);
	}
}

.animate-slide-in {
	animation: slide-in 0.3s ease-out;
}

/* Always respect reduced motion */
@media (prefers-reduced-motion: reduce) {
	.animate-slide-in {
		animation: none;
	}
}
```

### RTL Support - Use Logical Properties

```html
<!-- ✅ Correct -->
<div class="ms-4 me-2 ps-3 pe-1 text-start">
	<!-- ❌ Wrong (breaks RTL) -->
	<div class="mr-2 ml-4 pr-1 pl-3 text-left"></div>
</div>
```

| Physical    | Logical                |
| ----------- | ---------------------- |
| `ml-*`      | `ms-*` (margin-start)  |
| `mr-*`      | `me-*` (margin-end)    |
| `pl-*`      | `ps-*` (padding-start) |
| `pr-*`      | `pe-*` (padding-end)   |
| `left-*`    | `start-*`              |
| `right-*`   | `end-*`                |
| `text-left` | `text-start`           |

### Class Merging

```svelte
<script lang="ts">
	import { cn } from '$lib/utils';

	interface Props {
		class?: string;
		variant?: 'default' | 'destructive';
	}

	let { class: className, variant = 'default' }: Props = $props();
</script>

<button class={cn('rounded px-4 py-2', variant === 'destructive' && 'bg-red-500', className)}>
	<slot />
</button>
```

### Animations & Reduced Motion

Always respect `prefers-reduced-motion` for accessibility. Use Tailwind's `motion-safe:` variant for animations:

```html
<!-- ✅ Correct - only animates if user hasn't requested reduced motion -->
<div class="motion-safe:duration-300 motion-safe:animate-in motion-safe:fade-in">
	<!-- ❌ Wrong - animates regardless of user preference -->
	<div class="duration-300 animate-in fade-in"></div>
</div>
```

For custom CSS animations in `src/styles/animations.css`, disable them in the reduced motion media query:

```css
@media (prefers-reduced-motion: reduce) {
	.animate-custom {
		animation: none;
	}
}
```

---

## Internationalization (i18n)

### Message Keys Convention

```
{domain}_{feature}_{element}
```

Examples:

- `auth_login_title`
- `profile_personalInfo_firstName`
- `nav_dashboard`

### Usage

```svelte
<script lang="ts">
	import * as m from '$lib/paraglide/messages';
</script>

<h1>{m.auth_login_title()}</h1>
<Label>{m.profile_personalInfo_firstName()}</Label>

<svelte:head>
	<title>{m.meta_profile_title()}</title>
	<meta name="description" content={m.meta_profile_description()} />
</svelte:head>
```

### Adding New Keys

Edit `src/messages/en.json` and `src/messages/cs.json`:

```json
{
	"profile_newFeature_label": "New Feature"
}
```

---

## Routes Structure

```
src/routes/
├── (app)/              # Authenticated routes
│   ├── +layout.svelte  # App shell (sidebar, header)
│   ├── +page.svelte    # Dashboard
│   ├── profile/
│   └── settings/
│
├── (public)/           # Public routes
│   └── login/
│
├── api/                # API proxy routes
│   └── [...path]/      # Proxy to backend
│
├── +layout.svelte      # Root layout
└── +error.svelte       # Error page
```

---

## Data Fetching Patterns

### Server-Side (Recommended for Initial Load)

Use `+page.server.ts` for data needed on page load:

```typescript
// src/routes/(app)/profile/+page.server.ts
import { createApiClient } from '$lib/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies, fetch }) => {
	const client = createApiClient(fetch, cookies);
	const { data } = await client.GET('/api/users/me');

	return {
		user: data
	};
};
```

```svelte
<!-- src/routes/(app)/profile/+page.svelte -->
<script lang="ts">
	let { data } = $props(); // Typed automatically from load function
</script>

<h1>Welcome, {data.user?.firstName}</h1>
```

**When to use server-side:**

- Initial page data (SEO-friendly, no loading spinners)
- Data that requires authentication cookies
- Data that should be available before hydration

### Client-Side (For User Interactions)

Use `browserClient` for actions triggered by user interaction:

```svelte
<script lang="ts">
	import { browserClient } from '$lib/api';

	let isLoading = $state(false);

	async function handleSave() {
		isLoading = true;
		const { response, error } = await browserClient.PATCH('/api/users/me', {
			body: { firstName, lastName }
		});
		isLoading = false;
	}
</script>
```

**When to use client-side:**

- Form submissions
- User-triggered actions (delete, update, create)
- Polling or real-time updates
- Data that changes frequently after page load

### Combining Both

Load initial data server-side, then update client-side:

```svelte
<script lang="ts">
	import { browserClient } from '$lib/api';

	let { data } = $props();
	let user = $state(data.user); // Initialize from server data

	async function refresh() {
		const { data: updated } = await browserClient.GET('/api/users/me');
		if (updated) user = updated;
	}
</script>
```

---

## Quality Checklist

Before marking any task complete, run ALL of these:

```bash
npm run format   # Prettier
npm run lint     # ESLint
npm run check    # Svelte type check
npm run build    # Production build
```

If any fail, fix the issues before proceeding.

---

## Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add phone input to registration form
fix: display specific validation errors in RegisterDialog
refactor: move shake utilities to state folder
chore: update dependencies
docs: improve README
```

Keep commits atomic and focused on a single change.

### Before Every Commit

Review your changes:

1. **Consistency**: Do the changes align with existing patterns in the codebase?
2. **Completeness**: Are all related files updated (components, types, translations)?
3. **Dead code**: When deleting or refactoring, check for orphaned code:
   - Unused imports
   - Unused translation keys in `messages/*.json`
   - Unused components or utilities
   - Orphaned type definitions
4. **Naming**: Do new files/functions follow existing conventions?

5. **Always verify** the solution works before committing:

```bash
npm run format   # Fix formatting
npm run lint     # Check for lint errors
npm run check    # TypeScript/Svelte type check
npm run build    # Verify production build succeeds
```

If any command fails, fix the issues before committing. This prevents broken commits from entering the repository.

## Common Patterns

### Form with Validation

```svelte
<script lang="ts">
	import { createFieldShakes } from '$lib/state';
	import { isValidationProblemDetails, mapFieldErrors, browserClient } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import * as m from '$lib/paraglide/messages';

	let isLoading = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		fieldErrors = {};

		const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
			body: { firstName, lastName }
		});

		if (response.ok) {
			toast.success(m.profile_updateSuccess());
		} else if (isValidationProblemDetails(apiError)) {
			fieldErrors = mapFieldErrors(apiError.errors);
			fieldShakes.triggerFields(Object.keys(fieldErrors));
		} else {
			toast.error(m.profile_updateError());
		}

		isLoading = false;
	}
</script>

<form onsubmit={handleSubmit}>
	<Input
		bind:value={firstName}
		class={fieldShakes.class('firstName')}
		aria-invalid={!!fieldErrors.firstName}
	/>
	{#if fieldErrors.firstName}
		<p class="text-xs text-destructive">{fieldErrors.firstName}</p>
	{/if}
</form>
```

### Dialog Component

```svelte
<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';

	let { open = $bindable(false) }: { open?: boolean } = $props();
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Title</Dialog.Title>
			<Dialog.Description>Description</Dialog.Description>
		</Dialog.Header>
		<!-- Content -->
		<Dialog.Footer>
			<Button>Save</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
```

---

## Don'ts

- ❌ Don't use `export let` (use `$props()`)
- ❌ Don't use `any` type
- ❌ Don't use physical CSS properties (`ml-`, `mr-`, `left-`, `right-`)
- ❌ Don't import server config from the barrel (`$lib/config`)
- ❌ Don't leave components in `src/lib/components/` root
- ❌ Don't skip the quality checklist
- ❌ Don't mix reactive state (`.svelte.ts`) with pure utils
