---
description: TEMPLATE-ONLY - verify changes against a fully running stack by instantiating the template, checking in the browser, then reverting every init artifact. Use whenever a change needs visual or runtime verification in the real app. Removed by init.sh, do not reference from skills that survive init.
---

Verify template changes in a real running app. The template's placeholder form (`MyProject`) cannot run the full stack; a throwaway instantiation can. Nothing from the instantiation may leak into commits.

When polishing UI in the template, run `/polish-ui` step 5 (browser verification) inside this loop.

## The loop

1. **Commit first**: the real (pre-init, template-form) changes must be committed before anything else - the revert below destroys uncommitted work. Capture the SHA: `PRE_INIT_SHA=$(git rev-parse HEAD)`.
2. **Snapshot Docker**: note existing containers/volumes (`docker ps -aq`, `docker volume ls -q`) so cleanup removes only what this run creates.
3. **Instantiate**: `./init.sh --name VerifyApp --yes --no-commit --no-build --no-aspire`. Do NOT add `--no-migration` - the initial migration is what makes the stack actually run (it builds WebApi even with `--no-build`).
4. **Run**: `dotnet run --project src/backend/VerifyApp.AppHost` in the background. The frontend serves at the base port (default `http://localhost:13000`); confirm with a probe, do not assume.
5. **Verify**: drive the browser (Playwright), capture screenshots at all required breakpoints and both themes. Save screenshots OUTSIDE the repo (scratchpad) - the revert deletes untracked repo files.
6. **Preview fixes cheaply**: small copy/style fixes can be edited directly in the instantiated tree to preview live before teardown - these edits are throwaway, the real fix is applied pre-init afterwards.
7. **Teardown**:
   - Stop Aspire.
   - `git reset --hard "$PRE_INIT_SHA"` (the project hook blocks a bare `git reset --hard` - always pass the explicit target).
   - Delete untracked init artifacts: verify `git ls-files --others --exclude-standard` lists ONLY instantiation files, then remove them (the project hook blocks `git clean` - use the list + `rm`).
   - Delete gitignored leftovers too: the instantiation's `bin`/`obj` output is invisible to the untracked listing. Remove the renamed project trees wholesale (`rm -rf src/backend/<Name>.* src/backend/tests/<Name>.*`), then `find src/backend -type d -empty -delete`.
   - Remove the Docker containers/volumes created since the snapshot (Aspire volumes are named `<name>-db-data`, `<name>-storage-data`).
8. **Apply findings pre-init**: fixes discovered during verification go into the template files (normal delegation rules), then commit. Repeat the loop if the fix needs visual confirmation.

## Evidence

Commit the meaningful screenshots under `docs/sessions/assets/<date>-<topic>/` and embed them in the PR description with SHA-pinned raw URLs (`https://raw.githubusercontent.com/<owner>/<repo>/<full-sha>/docs/sessions/assets/...`) so they survive branch deletion after squash-merge.
