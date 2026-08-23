# Design Tokens Quick Reference (for Design Review)

## Semantic Colors (use these, never hardcoded)

| Token | Usage |
|---|---|
| `bg-background` | Page/card backgrounds |
| `bg-muted` | Subtle background sections |
| `bg-primary` | Primary action backgrounds |
| `bg-destructive` | Danger action backgrounds |
| `text-foreground` | Primary text |
| `text-muted-foreground` | Secondary/helper text |
| `text-primary` | Links, emphasized text |
| `text-destructive` | Error text |
| `border` | Default borders |
| `border-destructive` | Error state borders |
| `ring` | Focus ring color |

## Spacing Scale

| Mobile | Tablet | Desktop |
|---|---|---|
| `p-4` | `sm:p-6` | `lg:p-8` |
| `gap-2` | `sm:gap-3` | `lg:gap-4` |

## Breakpoints

| Name | Min Width | Usage |
|---|---|---|
| `sm:` | 640px | Small tablets |
| `md:` | 768px | Tablets |
| `lg:` | 1024px | Desktop |
| `xl:` | 1280px | Wide desktop |
| `2xl:` | 1536px | Ultrawide |

## Touch Targets

- Minimum 44px (`min-h-11`) on all interactive elements (CLAUDE.md hard rule)

## Typography Minimums

- Smallest allowed: `text-xs` (12px)
- Body text: `text-sm` (14px) or `text-base` (16px)

## Button Layout Standard

```
Wrapper: flex flex-col gap-2 sm:flex-row sm:justify-end
Button:  w-full sm:w-auto (default size)
```

## Dialog Constraints

- No scrollbars - content must fit viewport
- Width: `max-w-[calc(100%-2rem)]` mobile, `sm:max-w-lg` desktop
- Grids: start `grid-cols-1`, add responsive breakpoints

## Content Layout

- Max width: `max-w-7xl mx-auto`
- Content grids: `lg:grid-cols-2` (not `xl:`)
- Full height: `h-dvh` (not `h-screen`)

## Physical -> Logical CSS Mapping

| Avoid | Use Instead |
|---|---|
| `ml-*` / `mr-*` | `ms-*` / `me-*` |
| `pl-*` / `pr-*` | `ps-*` / `pe-*` |
| `left-*` / `right-*` | `start-*` / `end-*` |
| `text-left` / `text-right` | `text-start` / `text-end` |
| `border-l` / `border-r` | `border-s` / `border-e` |
| `space-x-*` | `gap-*` |
