---
description: Reviews a Dependabot PR and evaluates whether the dependency update is safe to merge. Use when a Dependabot PR is opened, when evaluating dependency updates, or when asked to check if a version bump is safe.
context: fork
agent: Explore
allowed-tools: Read, Glob, Grep, Bash, WebFetch
argument-hint: "[PR number or URL]"
---

Reviews a Dependabot PR and evaluates whether it is safe to merge.

Argument: PR number or URL.

**Open Dependabot PRs:**
!`gh pr list --author 'app/dependabot' --json number,title --jq '.[] | "#\(.number) \(.title)"' 2>/dev/null | head -10 || echo "(none found)"`

## Steps

1. Fetch PR metadata: `gh pr view {number} --json number,title,headRefName,body,labels,author`
2. **Verify the author is `app/dependabot`.** If it is not, STOP and report - this skill fetches and builds the PR's code locally, and that is only acceptable for Dependabot-authored version bumps. Never proceed for an arbitrary PR number.
3. Get the diff: `gh pr diff {number}`
4. Identify the package, old version, new version, and version bump type (patch / minor / major)

## Determine scope

| Signal | How to check |
|---|---|
| Backend (.NET) | Diff touches `Directory.Packages.props` |
| Frontend (pnpm) | Diff touches `package.json` / `pnpm-lock.yaml` |
| Dev-only dependency | Package appears only in test projects, build tooling, or `devDependencies` |
| Production dependency | Package is imported/used in shipped code |

## Evaluate risk

### 1. Version bump type

- **Patch** (x.y.Z) - bug fixes, low risk
- **Minor** (x.Y.0) - new features, backwards-compatible by semver, low-medium risk
- **Major** (X.0.0) - potentially breaking, high risk - requires careful review

### 2. Changelog review

- Read the Dependabot PR body for release notes / changelog links
- If the PR body includes a changelog summary, check for breaking changes, deprecations, and behavioral changes
- For major bumps: `WebFetch` the changelog URL if provided

### 3. Usage impact

- Search the codebase for direct imports/usages of the package:
  - .NET: `grep -r "using.*{PackageNamespace}" src/backend/` and check `.csproj` references
  - Frontend: `grep -r "from ['\"].*{package}" src/frontend/src/`
- Check if the package is a transitive dependency (only in lock file, not directly referenced)
- For major bumps: check if any API surface we use was deprecated or removed

### 4. Test verification

Never `gh pr checkout` - that would switch the user's working tree. Test in a disposable worktree:

```bash
git fetch origin pull/{number}/head
git worktree add /tmp/dependabot-{number} FETCH_HEAD
```

- Backend: `dotnet build /tmp/dependabot-{number}/src/backend/MyProject.slnx && dotnet test /tmp/dependabot-{number}/src/backend/MyProject.slnx -c Release`
- Frontend: `cd /tmp/dependabot-{number}/src/frontend && pnpm install --ignore-scripts && pnpm run test && pnpm run check`
  - `--ignore-scripts` is deliberate: package lifecycle scripts are the main local code-execution risk of building PR content. If the updated package genuinely needs its install scripts to function, do NOT run them - verdict is NEEDS MANUAL REVIEW.
- Afterwards: `git worktree remove --force /tmp/dependabot-{number}`
- If tests pass, that's a strong signal. If they fail, identify whether the failure is related to the update.

## Output format

```
## Dependabot Review: {package} {old} -> {new}

**Bump type**: patch / minor / major
**Scope**: backend / frontend
**Dependency type**: production / dev-only
**Test result**: PASS / FAIL

### Risk assessment

{Summary of findings - changelog highlights, breaking changes, usage impact}

### Verdict: SAFE TO MERGE / NEEDS MANUAL REVIEW / DO NOT MERGE

{Reasoning for verdict}
```

### Verdict criteria

| Verdict | When |
|---|---|
| **SAFE TO MERGE** | Patch/minor bump, tests pass, no breaking changes in changelog, no security advisories |
| **NEEDS MANUAL REVIEW** | Major bump with passing tests, or minor bump with deprecation warnings, or unfamiliar package with wide usage |
| **DO NOT MERGE** | Tests fail due to the update, known breaking changes that affect our usage, security concerns in the new version |

## Rules

- Research only - never modify the user's working tree; file side effects stay inside the disposable worktree. Building/testing still EXECUTES code from the PR (MSBuild targets, test code) with your privileges - that is why the author check and `--ignore-scripts` are mandatory, and why this skill is for Dependabot PRs only
- Always run the test suite - never skip it
- When in doubt, verdict is NEEDS MANUAL REVIEW, not SAFE TO MERGE
- A passing test suite does not override known breaking changes in the changelog
