---
description: Reviews frontend components for design quality and UI/UX standards. Use when checking component styling, responsiveness, accessibility, or when asked to review a design.
context: fork
agent: Explore
allowed-tools: Read, Glob, Grep, Bash
argument-hint: "[file path, component name, or glob pattern]"
---

Reviews frontend components for design quality, UI/UX best practices, and project standards.

Argument: file path, component name, or glob pattern. If omitted, reviews all files changed on the current branch vs the repo default branch.

## References

- [Design tokens and standards](references/design-tokens.md) - colors, spacing, breakpoints, touch targets

## Steps

1. **Resolve scope**: If argument provided, find matching `.svelte` files. If omitted, get changed files via `git diff $(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null || echo origin/master) --name-only -- '*.svelte'`
2. **Read every file in scope** in full - understand the complete component, not just fragments
3. **Read the [design tokens reference](references/design-tokens.md)** for project standards
4. **Check the parent layout/page** that renders each component - understand the context it lives in
5. **Cross-reference related components** - check siblings in the same feature folder for consistency

## Design Checklist

### Responsive Design

- [ ] Mobile-first: base styles target 320px, then `sm:` / `md:` / `lg:` / `xl:` breakpoints
- [ ] No hardcoded widths that break on small screens (except `max-w-*` constraints)
- [ ] Padding scales with breakpoints (`p-4 sm:p-6 lg:p-8`) - flat large padding wastes mobile space
- [ ] Content grids use `lg:grid-cols-2` (not `xl:`) since `max-w-7xl` ensures sufficient width
- [ ] Dialog grids start with `grid-cols-1` base before responsive breakpoints
- [ ] Full-height layouts use `h-dvh` not `h-screen`
- [ ] Flex children with text have `min-w-0`, icons/badges have `shrink-0`
- [ ] Text doesn't overflow containers - check long names, emails, URLs with `truncate` or `break-all`

### Touch & Interaction

- [ ] All interactive elements meet 44px minimum touch target (`min-h-11` or `h-11`)
- [ ] Buttons use default size with `w-full sm:w-auto` pattern
- [ ] Button wrapper uses `flex flex-col gap-2 sm:flex-row sm:justify-end`
- [ ] No `size="sm"` or `size="lg"` on action/submit buttons
- [ ] Action buttons are right-aligned (not left-aligned or centered)
- [ ] Clickable text elements (links styled as text) still meet touch target minimums
- [ ] Disabled states are visually distinct and non-interactive

### Logical CSS (RTL Support)

- [ ] No physical margin/padding: `ms-*`/`me-*`/`ps-*`/`pe-*` instead of `ml-*`/`mr-*`/`pl-*`/`pr-*`
- [ ] No physical positioning: `start-*`/`end-*` instead of `left-*`/`right-*`
- [ ] No `text-left`/`text-right` - use `text-start`/`text-end`
- [ ] No `border-l`/`border-r` - use `border-s`/`border-e`
- [ ] No `space-x-*` - use `gap-*`

### Theming & Colors

- [ ] All colors use semantic design tokens (`bg-background`, `text-muted-foreground`, `border-destructive`)
- [ ] No hardcoded colors (`text-gray-500`, `bg-white`, `#fff`, `rgb(...)`)
- [ ] UI works in both light and dark mode - check contrast, borders, shadows
- [ ] Muted backgrounds use `/40` or similar opacity variants, not solid colors that clash

### Typography & Spacing

- [ ] Minimum font size is `text-xs` (12px) - nothing smaller
- [ ] Consistent spacing within the component and with sibling components
- [ ] Headings use consistent sizing per hierarchy level across the app
- [ ] No orphaned labels or empty states without user guidance

### Dialogs & Modals

- [ ] No scrollbars in dialogs - content fits the viewport
- [ ] No `overflow-y-auto` on dialog containers
- [ ] Compact spacing and responsive sizing to avoid scroll
- [ ] Width constraint: `max-w-[calc(100%-2rem)]` mobile, `sm:max-w-lg` desktop (or similar)

### Safe Area & Viewport

- [ ] Fixed/sticky elements account for `env(safe-area-inset-*)` where relevant
- [ ] Bottom-fixed elements use `pb-[max(...,env(safe-area-inset-bottom,0px))]` pattern
- [ ] Animations use `motion-safe:` prefix

### Cross-Browser Compatibility

Check for patterns that break across the three main browser engines:

**WebKit (Safari / iOS Safari)**
- [ ] No `-webkit-` prefixed properties without standard fallback
- [ ] `dvh`/`svh` units used instead of `vh` for full-height layouts (iOS URL bar)
- [ ] `gap` on flex containers (supported since Safari 14.1 - verify no older target)
- [ ] No `backdrop-filter` without `-webkit-backdrop-filter` fallback if used
- [ ] `overscroll-behavior` present on scroll containers (Safari 16+)
- [ ] Date/time inputs work (Safari has limited `<input type="date">` styling)

**Blink (Chrome / Edge / Opera)**
- [ ] No reliance on Chrome-only CSS (`:has()` is fine - supported everywhere now)
- [ ] Scrollbar styling uses both `::-webkit-scrollbar` and `scrollbar-width`/`scrollbar-color` if customized

**Gecko (Firefox)**
- [ ] `text-overflow: ellipsis` works (Firefox requires `overflow: hidden` + `white-space: nowrap` explicitly)
- [ ] CSS `@property` usage has fallbacks (limited Firefox support before v128)
- [ ] No `-moz-` prefixed properties without standard fallback
- [ ] Nested CSS `@layer` rules work (Firefox 97+)

**General**
- [ ] No CSS features below the project's browser target without fallbacks
- [ ] `env(safe-area-inset-*)` uses fallback values: `env(safe-area-inset-top, 0px)` not `env(safe-area-inset-top)`
- [ ] Font stacks include fallbacks for system fonts across OS (Apple, Windows, Linux)
- [ ] `prefers-color-scheme` and `prefers-reduced-motion` media queries used correctly

### Consistency

- [ ] Patterns match existing components in the same feature folder
- [ ] Card layouts, form structures, error handling, and button placement match the rest of the app
- [ ] Loading states follow existing patterns (spinner icon with `animate-spin`)
- [ ] Error display follows existing patterns (`getErrorMessage` + toast or inline `Alert`)
- [ ] Empty states are handled with user-friendly messaging

### Accessibility

- [ ] Interactive elements have visible focus styles (inherited from shadcn unless custom)
- [ ] Icon-only buttons have `aria-label` or `sr-only` text
- [ ] Form inputs have associated labels
- [ ] Color is not the only indicator of state (add icons or text alongside color changes)
- [ ] Semantic HTML: `<nav>`, `<main>`, `<header>`, `<section>` used appropriately

## Output Format

Report findings per component as:

- **PASS** - what meets standards (brief)
- **FAIL** - violations that must be fixed (with file path, line number, and fix)
- **WARN** - suggestions for improvement (not blockers)

End with a summary: total components reviewed, FAIL count, WARN count, and overall verdict.

## Rules

- Research only - do NOT modify any files
- Read the full component source, not just snippets - context matters
- Compare against sibling components for consistency violations
- Check the actual rendered context (parent layout) to understand spacing/sizing in practice
- Be strict on logical CSS, touch targets, and theme tokens - these are hard rules
- Be practical on spacing/sizing - flag only clear violations, not subjective preferences
