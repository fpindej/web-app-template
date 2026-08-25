---
description: Design-quality workflow that combines design taste, aesthetic direction, image-to-code, a Web Interface Guidelines audit, and real-browser verification. Use when building or polishing a UI surface, implementing a mockup/screenshot, or when asked to make a page "look great".
argument-hint: "[route, component path, or image path] [optional style: shadcn|clean|premium]"
---

Polish or build a frontend surface to a high design standard, then prove it in a real browser.

This skill composes user-level skills installed at `~/.claude/skills/`: `design-taste-frontend`, `image-to-code`, `web-design-guidelines`, `playwright-cli`, and the aesthetic packs `shadcn`, `clean`, `premium`. If any are missing, install them first:

```bash
npx skills add leonxlnx/taste-skill --skill design-taste-frontend --skill image-to-code -g -a claude-code -y
npx skills add vercel-labs/agent-skills --skill web-design-guidelines -g -a claude-code -y
npx skills add microsoft/playwright-cli --skill playwright-cli -g -a claude-code -y
npx skills add bergside/awesome-design-skills --skill shadcn --skill clean --skill premium -g -a claude-code -y
```

## Arguments

- **Target**: a route (`/settings`), a component path, or an image/mockup file. If omitted, ask what surface to work on.
- **Style** (optional): `shadcn` (default - matches this app), `clean`, or `premium`. The style pack informs direction only; the app's existing tokens and components always win. Unified UX is a hard rule - never make one page feel like a different product.

## Steps

1. **Set design direction**: Invoke the `design-taste-frontend` skill and the chosen style pack skill. Distill their guidance into a short brief scoped to the target: hierarchy, spacing rhythm, typography, restraint. Filter out anything that conflicts with project hard rules (shadcn-svelte components, existing tokens, logical CSS, 44px touch targets, no overflow).

2. **If the target is an image or mockup**: Invoke the `image-to-code` skill for its analysis method (section-by-section, readable reference views, no cards-inside-cards). Translate the output into Svelte 5 terms - the implementation must use runes, `interface Props` + `$props()`, shadcn-svelte components, and feature folders. Ignore the skill's instructions about generating images with Codex tooling; analyze the provided image instead.

3. **Implement via delegation**: Pass the distilled brief to `frontend-engineer` (Delegation Rule applies - the orchestrator does not write `src/` code). Include the specific taste/style constraints in the agent prompt so the direction survives the handoff.

4. **Audit**: Invoke the `web-design-guidelines` skill against the changed files for Web Interface Guidelines compliance (accessibility, focus states, interaction details). Also run `/review-design` for project-specific standards. Fix findings via the engineer agent; loop until clean.

5. **Verify in a real browser**: Invoke the `playwright-cli` skill (or use the project Playwright MCP) against the running app (Aspire full stack, or the frontend dev server). Probe for the frontend URL - do not assume a port. Screenshot the target at the canonical breakpoints from `/review-design`'s design-tokens reference (320/375/768/1024/1440/2560), plus landscape on mobile. Check: no scrollbars in dialogs/modals, no horizontal overflow, touch targets >= 44px, both light and dark themes. Save screenshots to the scratchpad first.

6. **Standard review pass**: Run `frontend-reviewer` + `ux-designer` in parallel per CLAUDE.md, then run `/verify` and commit.

7. **PR with visual evidence**: When creating the PR, commit the meaningful screenshots under `docs/sessions/assets/<date>-<topic>/` on the branch and embed them in the PR description, grouped by breakpoint and theme. Use SHA-pinned URLs (`https://raw.githubusercontent.com/<owner>/<repo>/<full-sha>/docs/sessions/assets/...`) so images survive branch deletion after squash-merge; raw URLs render only for public repos - in a private repo use `https://github.com/<owner>/<repo>/blob/<full-sha>/<path>?raw=1`.

## Notes

- Steps 1-2 produce a brief, not code. All `src/frontend/` edits go through engineer agents unless the user overrides.
- Screenshots from step 5 are evidence - if a breakpoint looks broken, that is a finding to fix, not a caveat to report.
