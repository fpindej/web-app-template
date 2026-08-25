# PR Body Template

Use this structure for all pull request descriptions.

```markdown
## Summary
- {Change 1 - what and why}
- {Change 2 - what and why}
- {Change 3 - what and why}

## Breaking Changes
None / {describe if any, including migration steps}

## Screenshots
{Only for UI-visible changes - otherwise omit the section. Embed the
screenshots committed under docs/sessions/assets/, grouped by breakpoint
and theme, as tables with before/after pairs where relevant.}

## Test Plan
- [ ] {Verification step 1}
- [ ] {Verification step 2}
- [ ] Backend: `dotnet build && dotnet test -c Release`
- [ ] Frontend: `pnpm run test && pnpm run check`
```

## Guidelines

- **Summary**: Bullet points, focus on "what changed and why" not "which files"
- **Screenshots**: If the branch adds files under `docs/sessions/assets/`, embed them with SHA-pinned URLs: `https://raw.githubusercontent.com/{owner}/{repo}/{full-sha}/{path}` (public repos; branch URLs 404 after squash-merge deletes the branch). Private repos: `https://github.com/{owner}/{repo}/blob/{full-sha}/{path}?raw=1`.
- **Breaking Changes**: Required section. "None" if no breaking changes. If breaking, describe the migration path.
- **Test Plan**: Concrete steps a reviewer can follow. Always include the verification commands.
- Keep the title under 70 chars, Conventional Commit format
- Add labels: `backend`, `frontend`, `feature`, `bug`, `security`, `documentation`
- For stacked PRs: set `--base` to the parent branch
