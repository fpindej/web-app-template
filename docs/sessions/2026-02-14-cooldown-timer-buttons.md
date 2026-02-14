# Cooldown Timer on Rate-Limited Buttons

**Date**: 2026-02-14
**Scope**: Show a visible countdown timer on all buttons during rate-limit cooldown

## Summary

Previously, rate-limited buttons were silently disabled via `cooldown.active` with no visual indication of why or how long the user must wait. This change replaces the button label with a "Wait Xs" countdown (using `cooldown.remaining`) across all 11 components that participate in rate-limit handling. Added `common_waitSeconds` i18n key in both English and Czech.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/messages/en.json` | Added `common_waitSeconds` key | i18n support for countdown label |
| `src/frontend/src/messages/cs.json` | Added `common_waitSeconds` key | Czech translation |
| `src/frontend/src/lib/components/auth/LoginForm.svelte` | Show countdown on submit button | Login: 3 attempts / 30s window |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | Show countdown on register button | Registration: 2 attempts / 15s window |
| `src/frontend/src/lib/components/profile/ProfileForm.svelte` | Show countdown on save button | Sensitive operation |
| `src/frontend/src/lib/components/profile/AvatarDialog.svelte` | Show countdown on remove + save buttons | Sensitive operation |
| `src/frontend/src/lib/components/settings/ChangePasswordForm.svelte` | Show countdown on submit button | Sensitive: 2 attempts / 1min window |
| `src/frontend/src/lib/components/settings/DeleteAccountDialog.svelte` | Show countdown on confirm button | Sensitive operation |
| `src/frontend/src/lib/components/admin/UserManagementCard.svelte` | Show countdown on 4 action buttons | AdminMutations: 3 attempts / 45s window |
| `src/frontend/src/lib/components/admin/CreateRoleDialog.svelte` | Show countdown on create button | AdminMutations policy |
| `src/frontend/src/lib/components/admin/JobActionsCard.svelte` | Show countdown on 4 action buttons | AdminMutations policy |
| `src/frontend/src/routes/(app)/admin/roles/[id]/+page.svelte` | Show countdown on 3 action buttons | AdminMutations policy |
| `src/frontend/src/routes/(app)/admin/jobs/+page.svelte` | Show countdown on restore button | AdminMutations policy |

## Decisions & Reasoning

### Button label replacement vs separate indicator

- **Choice**: Replace the button text itself with "Wait Xs" during cooldown
- **Alternatives considered**: Separate badge/tooltip next to the button; progress bar inside button
- **Reasoning**: Replacing the label is the simplest approach, requires no layout changes, and is immediately obvious. The button is already disabled, so repurposing its label for the countdown is natural.

### Per-component cooldown state vs global store

- **Choice**: Keep cooldown state per-component (lost on navigation)
- **Alternatives considered**: Global store keyed by rate-limit policy name
- **Reasoning**: When users navigate away and return, they get one extra failed request before the timer restarts — acceptable trade-off. The server-side rate limit is the real protection; the client timer is a UX courtesy. A global store would add complexity for minimal benefit.

### Combined `{:else if}` for icon+spinner buttons

- **Choice**: Merge `cooldown.active` check with existing `isLoading` conditional into `{#if cooldown.active}...{:else if isLoading}...{:else}` three-way branch
- **Alternatives considered**: Wrapping entire button content in an outer `{#if cooldown.active}` with duplicated icon+label in `{:else}`
- **Reasoning**: Three-way branch avoids duplicating the label text. The trade-off is that the icon (e.g., Save, Lock) is duplicated in the `{:else if}` and `{:else}` branches, but since those are small single-line SVG components this is acceptable.

## Follow-Up Items

- [ ] None — feature complete
