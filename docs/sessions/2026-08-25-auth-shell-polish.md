# Auth Shell Brand Panel Polish

**Date**: 2026-08-25
**Scope**: Design-quality pass on the shared auth shell (login, register, forgot/reset password, 2FA) via the new `/polish-ui` workflow

## Summary

Recomposed the decorative brand panel in `AuthShell.svelte` from a bare wordmark over two glow blobs into an intentional brand moment (wordmark + tagline + masked dot-grid depth layer), unified heading typography across all auth forms, and fixed a brand-name duplication on the login page found by design review. Verified visually against a fully running stack using the init-verify-revert loop, with screenshots committed as evidence.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/auth/AuthShell.svelte` | Panel: added tagline under wordmark, added `dot-grid-fade` layer, centered composition | Bare app name over generic glows read as placeholder |
| `src/frontend/src/styles/utilities.css` | New `.dot-grid-fade` utility (1px foreground dots at 8%, elliptical mask) | Token-driven depth that adapts to both themes |
| `src/frontend/src/messages/{en,cs}/auth.json` | New `auth_shell_tagline` key; `auth_login_title` no longer interpolates the app name | Panel tagline; fix duplicate branding at lg+ |
| `src/frontend/src/lib/components/auth/LoginForm.svelte` | `tracking-tight` on h1; title call without `{ name }` | Typography consistency; duplication fix |
| `RegisterForm/ForgotPasswordForm/ResetPasswordForm/TwoFactorStep.svelte` | `tracking-tight` on all h1s (11 occurrences total) | Identical heading cut across the whole auth flow |
| `docs/sessions/assets/2026-08-25-auth-shell-polish/` | 8 verification screenshots | Visual evidence from the running app |
| `.claude/skills/polish-ui/SKILL.md` | Added PR-screenshot step; verification step points to `init-verify` when present | Make the workflow repeatable without baking template-only steps into a skill that ships to consumers |
| `.claude/skills/init-verify/SKILL.md` | New template-only skill for the init-verify-revert loop | Runtime verification needs an instantiated copy; consumers never need this |
| `init.sh` / `init.ps1` | Added `.claude/skills/init-verify` to template cleanup dirs | The skill must not survive init |

## Decisions & Reasoning

### Login heading drops the app name

- **Choice**: `auth_login_title` becomes "Welcome back" / "Vitejte zpet"; the panel owns the brand name.
- **Alternatives considered**: keep the h1 and replace the panel wordmark with a logo mark.
- **Reasoning**: ux-designer review blocked on the same brand string appearing twice in one viewport at lg+. The generic welcome is the standard pattern, keeps mobile (panel hidden) natural, and required no new assets. Verified live before adopting.

### Verification via init-verify-revert

- **Choice**: commit pre-init changes, run `./init.sh --name VerifyApp --yes --no-commit --no-build --no-aspire`, launch Aspire, screenshot at 375/768/1024/1440/2560 plus mobile landscape in both themes, then `git reset --hard`, delete untracked init artifacts, and remove the Docker volumes.
- **Reasoning**: the template's placeholder form cannot run the full stack; a throwaway instantiation gives real-backend verification (green health, enabled form) without leaking into commits. Docker was empty before the run and empty after.

### Dot-grid layer kept despite density concern

- **Choice**: keep `bg-primary/5` + dot grid + two glows.
- **Reasoning**: ux-designer flagged the four-layer stack as worth a live look; at 1024px in both themes the layers read as one quiet texture, so the evidence settled it.

## Follow-ups

- Consider echoing the sidebar's icon-badge brand mark (larger) in the auth panel so both brand touchpoints share one visual language (ux-designer WARN, non-blocking).
- `auth_shell_tagline` is deliberate template scaffolding copy; consumers should replace it with product copy after init.
- Optional: `translate="no"` on the panel wordmark (web-interface-guidelines nit, pre-existing pattern).
