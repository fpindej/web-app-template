# GitHub Copilot Instructions

You are an expert SvelteKit developer working on a production-grade application. Your goal is to maintain an "S-Tier" architecture, ensuring scalability, maintainability, and type safety.

## 1. Tech Stack & Core Principles

- **Framework:** SvelteKit (latest) with **Svelte 5 Runes** (`$state`, `$props`, `$effect`, `$derived`).
- **Language:** TypeScript (Strict mode).
- **Styling:** Tailwind CSS 4 (Native CSS variables, `@theme inline`).
- **UI Library:** Shadcn UI (headless components via `bits-ui`).
- **i18n:** `paraglide-js` (Type-safe, no runtime overhead).

## 2. Folder Structure Rules

- **UI Components:** strictly in `src/lib/components/ui/`. These must be "dumb" (presentational only) and decoupled from business logic.
- **Feature Components:** Group by domain in `src/lib/components/<domain>/` (e.g., `auth`, `profile`, `layout`).
- **Barrel Files:** ALWAYS create or update `index.ts` in component folders to allow clean imports.
  - ✅ `import { Header } from '$lib/components/layout';`
  - ❌ `import Header from '$lib/components/layout/Header.svelte';`
- **Routes:** Use Route Groups `(app)` for authenticated routes and `(public)` for open routes.
- **No Root Components:** Never leave `.svelte` files directly in `src/lib/components/`. Move them to a specific domain folder.

## 3. Coding Standards (Svelte 5)

- **Runes Only:**
  - Use `$props()` instead of `export let`.
  - Use `$state()` instead of top-level `let` for reactivity.
  - Use `$derived()` for computed values.
- **Composition:** Prefer component composition (slots/snippets) over complex prop drilling.
- **Type Safety:** No `any`. Define interfaces for all props and API responses.
- **Async Data:** Use `+layout.ts` or `+page.ts` for data requirements that must be resolved _before_ rendering (to prevent FOUC).

## 4. Styling Guidelines (Tailwind 4)

- **Configuration:** Use CSS variables in `src/routes/layout.css` for theming. Do not rely on a complex `tailwind.config.js`.
- **Logical Properties (RTL Support):** ALWAYS use logical properties for spacing and positioning.
  - `ml-*` → `ms-*` (Margin Start)
  - `mr-*` → `me-*` (Margin End)
  - `pl-*` → `ps-*` (Padding Start)
  - `pr-*` → `pe-*` (Padding End)
  - `left-*` → `start-*`
  - `right-*` → `end-*`
  - `text-left` → `text-start`
- **Shadcn:** Use the existing UI components in `$lib/components/ui`. Do not reinvent the wheel.

## 5. Internationalization (i18n)

- **Usage:** Use the `m` object: `{m.domain_feature_key()}`.
- **Meta Tags:** Always include SEO meta tags in `<svelte:head>` using localized strings for `title` and `description`.

## 6. Workflow & Quality Control

Before confirming a task is done, **ALWAYS** perform the following checks:

1.  **Format:** `npm run format` (Prettier)
2.  **Lint:** `npm run lint` (ESLint)
3.  **Type Check:** `npm run check` (Svelte Check)
4.  **Build:** `npm run build` (Vite Build)

## 7. Commit Strategy

- Use **Conventional Commits** (e.g., `feat:`, `fix:`, `refactor:`, `chore:`).
- Keep commits atomic and focused.
