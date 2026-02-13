Create a pull request for the current branch.

## Execution

Follow the conventions in **AGENTS.md → "Pull Requests"** section.

1. Check for uncommitted changes (`git status`). If any, ask whether to commit first.
2. Review ALL commits on the branch: `git log master..HEAD --oneline`
3. Push if needed: `git push -u origin $(git branch --show-current)`
4. Create PR with `gh pr create`:
   - **Title**: Conventional Commit format, under 70 chars
   - **Base**: `master`
   - **Labels**: Apply all relevant (`backend`, `frontend`, `feature`, `bug`, etc.)
   - **Body**: Summary, changes, breaking changes (if any), test plan, `Closes #N`

**Merge strategy** for this project is squash-and-merge only (see AGENTS.md).

Report the PR URL to the user.
