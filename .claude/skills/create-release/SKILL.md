---
description: Create a versioned GitHub release with changelog
user-invocable: true
argument-hint: "[major|minor|patch] or [vX.Y.Z]"
---

Creates a GitHub release with auto-generated release notes.

## Hard rules

- **Only release from master.** If not on master, switch first.
- **No Co-Authored-By lines** in commits.
- **Tag signing follows the developer's git config** (`tag.gpgSign`) - never pass `-s`/`-S` explicitly; fresh clones may have no signing key.
- Releases use **semantic versioning** (vMAJOR.MINOR.PATCH).

## Steps

1. Ensure you are on `master` and up to date: `git checkout master && git pull`

2. Determine the version:
   - If the user provided a version (e.g., `v1.2.3`), use it
   - If the user provided a bump type (`major`, `minor`, `patch`), calculate from the latest tag:
     ```bash
     git tag --list 'v*' --sort=-v:refname | head -1
     ```
   - If no argument and no tags exist, ask the user what version to use
   - If no argument but tags exist, default to `patch` bump

3. Check what changed since the last release:
   ```bash
   # If previous tag exists:
   git log <previous-tag>..HEAD --oneline
   # If first release:
   git log --oneline -50
   ```

4. Generate release notes by categorizing merged PRs and commits since the last tag. Group changes into these categories (omit empty categories):
   - **New Features** - `feat:` commits and feature PRs
   - **Security** - `security:` or security-related changes
   - **Bug Fixes** - `fix:` commits
   - **Improvements** - `refactor:`, `perf:` commits
   - **Documentation** - `docs:` commits
   - **Infrastructure** - `chore:`, `ci:`, `build:` commits

   Format each entry as: `- Description (#PR)` where possible.

   Add a brief intro paragraph summarizing the release highlights before the categories.

5. Create the tag and release:
   ```bash
   git tag -a <version> -m "Release <version>"
   git push origin <version>
   gh release create <version> --title "<version>" --notes "<release-notes>"
   ```

6. Report the release URL.

## Version Bumping Reference

Given `vMAJOR.MINOR.PATCH`:
- `major` - breaking changes or major milestones (v1.0.0 -> v2.0.0)
- `minor` - new features, no breaking changes (v1.0.0 -> v1.1.0)
- `patch` - bug fixes, docs, refactors (v1.0.0 -> v1.0.1)

## First Release

For the very first release (no prior tags), review the full project scope and write release notes that highlight the key capabilities of the project as shipped. This is the launch announcement.
