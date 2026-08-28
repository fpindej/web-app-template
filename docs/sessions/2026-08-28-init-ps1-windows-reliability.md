# init.ps1 Windows Reliability

**Date**: 2026-08-28
**Scope**: Fix Windows-only failures in `init.ps1`, restore behavioural parity with `init.sh`, and add CI that actually runs both scripts

## Summary

Windows users reported repeated failures running `init.ps1` while macOS and Linux users running `init.sh` reported none. The two scripts were kept in sync commit by commit (27 commits each, only one divergent), but nothing ever executed `init.ps1`: no CI job ran it, the `init-verify` skill runs `./init.sh` only, and the `- [ ] Test init.ps1 on Windows PowerShell 5.1` item from [2026-02-16](2026-02-16-init-deploy-script-fixes.md) was never checked off. The PowerShell port was written to mirror bash line for line, so every place PowerShell's semantics silently differ from bash's shipped as a latent bug.

Three root causes, not a list of unrelated defects:

1. **Error discipline was inverted.** `init.sh` contains errors locally (every fallible command is an `if` condition or ends `|| true`), so `set -e` almost never fires. `init.ps1` set `$ErrorActionPreference = "Stop"` globally and wrapped the body in a `try` whose only partner was `finally { Pop-Location }`. There was no `catch` anywhere in the file, so any non-terminating error became a script-killing raw .NET exception.
2. **Case-sensitive, literal POSIX tools were replaced by case-insensitive, regex-substituting PowerShell operators.** `sed`, `grep`, `[[ == ]]` and `tr` are literal and case-sensitive. `-replace`, `-match`, `-eq` and `.ToLower()` are none of those.
3. **Inert shell primitives were replaced by throwing .NET console APIs.** `read -rsn1` and `echo -en "\033[A\033[2K"` cannot fail; `[Console]::ReadKey` and `[Console]::SetCursorPosition` throw, and under (1) those throws were fatal.

The worst symptom was silent rather than loud. Running `.\init.ps1 -Name MyProjectApi` produced a solution where every project directory was named `myprojectapiApi.*`, and a password containing `$&` was written into `appsettings.Development.json` as something the user never typed, with exit code 0 both times.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `init.ps1` | Add `catch` to the script-wide `try` | Every failure surfaced as a raw .NET dump with no indication of which step failed |
| `init.ps1` | Bracket `docker info` with `$ErrorActionPreference = "Continue"` | On Windows PowerShell 5.1 native stderr becomes a terminating `NativeCommandError`, so a stopped Docker daemon killed the script before the friendly message could print |
| `init.ps1` | All seven placeholder substitutions use ordinal `String.Replace` instead of `-replace` | `-replace`'s second operand is a .NET substitution template: `$&`, `$1`, `$_` in a password were executed, not inserted. `$_` could splice whole file contents into a config value |
| `init.ps1` | Replace three `Get-ChildItem -Recurse` calls with a pruning walker (`Get-TemplateItems`) | Exclusions were a `Where-Object` on the *result*, so `node_modules` and `.git` were fully enumerated. Under `EAP=Stop` the first unreadable path or `PathTooLongException` aborted the run |
| `init.ps1` | `-Force` on the walker's `Get-ChildItem` | On Linux and macOS PowerShell treats dot-prefixed entries as hidden, so `.claude/`, `.github/` and `src/frontend/.env.example` were skipped entirely |
| `init.ps1` | Match exclusions against the path relative to the repo root | Absolute-path matching meant a repo cloned under any folder named `bin`, `obj` or `node_modules` excluded every file |
| `init.ps1` | `[int]::TryParse` with re-prompt for the base port | `[int]$portInput` on a typo raised a terminating conversion error and killed the run |
| `init.ps1` | `-cnotmatch`, `-ceq`, `-creplace`, ordinal `.Contains` throughout | `-notmatch` accepted lowercase names that `init.sh` rejects; `-eq "MyProject"` treated `Myproject` as a match and skipped the rename; the case-insensitive lowercase pass re-matched what the first pass had just written |
| `init.ps1` | Case-only renames routed through a temporary name | NTFS and APFS are case-insensitive, so `MyProject.WebApi` to `Myproject.WebApi` was a no-op |
| `init.ps1` | `git rev-parse --git-dir` probe, `Remove-Item` fallback, removal decoupled from `-NoCommit` | Cleanup was git-only. With no `.git` (ZIP download, `degit`) every `git rm` exited 128, nothing was deleted, and the script still printed `[OK] Template files removed` |
| `init.ps1` | Delete `init.ps1` inline; detached `-EncodedCommand` fallback only if the file is locked, with apostrophes escaped | The payload interpolated the repo path into a single-quoted string, so `C:\Users\O'Brien\...` broke it silently |
| `init.ps1` | Console-capability guard in `Read-Checklist` plus `try/catch` around `ReadKey` | `[Console]::ReadKey` throws in the ISE and whenever stdin is redirected; the script died at the checklist |
| `init.ps1` | Clear `WindowWidth - 1` columns in the redraw | Writing exactly `WindowWidth` characters wraps the cursor on the legacy console, cancelling the move up and stacking duplicate menus |
| `init.ps1` | Stream `dotnet build` / `dotnet test` instead of capturing into `$output` | `Write-Host $array` joins with `$OFS`, collapsing every compiler error onto one line |
| `init.ps1` | `.ToLowerInvariant()` in `ConvertTo-KebabCase` and `$NewNameLower` | Under `tr-TR` and `az-Latn-AZ`, `InvoiceHub` produced the slug `ınvoice-hub` with a dotless i, which is invalid in Docker volume and MinIO bucket names |
| `init.ps1` | Preserve UTF-8 BOMs; skip binaries by NUL-byte probe | `Set-FileContent` wrote UTF8-no-BOM unconditionally, stripping the BOM from 5 tracked files. The extension denylist was not a binary detector |
| `init.ps1` | Save and restore `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` | `$env:` is the process environment, so the setting outlived the script and silently unsecured every later `dotnet run` in that window |
| `init.ps1` | `-Help`, `ValueFromRemainingArguments` rejection of unknown flags | A non-advanced script silently swallows unbound arguments, so `--no-commit` was ignored and the script committed anyway |
| `init.ps1` | `-Yes` fails fast on a missing or invalid `-Name`, and on an out-of-range `-Port` | Both previously blocked on a prompt or span in a loop with no way to answer |
| `init.ps1` | Mask the password prompt, falling back to plain read when stdin is redirected | `init.sh` uses `read -sp`. `Read-Host -AsSecureString` terminates the host process outright on redirected stdin, so the fallback is required |
| `init.ps1` | `#Requires -Version 5.1` | Documents the supported floor; nothing in the script needs PowerShell 7 |
| `.github/workflows/init-scripts.yml` | New template-only workflow: parse and PSScriptAnalyzer on `init.ps1`, plus a real initialization smoke test | The root cause was that nothing executed `init.ps1` |
| `init.sh`, `init.ps1` | Add the new workflow to the template cleanup lists | It must not survive into generated projects |
| `README.md` | Note that PowerShell 5.1 and 7 are both supported; document the non-interactive flags | `-Yes` existed but appeared in no user-facing doc |
| `docs/troubleshooting.md` | Add Mark-of-the-Web and redirected-stdin entries | Both are common Windows entry points that were undocumented |

## Decisions & Reasoning

### Ordinal `String.Replace` over escaped `-replace`

- **Choice**: `$content.Replace($token, $value)`
- **Alternatives considered**: keep `-replace` and escape the replacement with `[System.Text.RegularExpressions.Regex]::Escape` (wrong: that escapes the *pattern*, not the substitution template) or hand-escape `$` as `$$`
- **Reasoning**: no substitution grammar means nothing to escape and no future footgun. It is also literal and case-sensitive, which is exactly what `sed` does in `init.sh`. Hand-escaping only `$` would still leave the case-insensitivity problem.

### Pruning walker over `Get-ChildItem -Recurse` with `-ErrorAction SilentlyContinue`

- **Choice**: an explicit queue-based walk that skips excluded directories during traversal
- **Alternatives considered**: keep `-Recurse` and only add `-ErrorAction SilentlyContinue`
- **Reasoning**: silencing the error fixes the abort but still walks tens of thousands of `node_modules` entries and still trips MAX_PATH on 5.1. Pruning matches what `grep --exclude-dir` does in `init.sh` and made a full run drop to about 4 seconds. `-Force` is safe here precisely because `.git` is excluded by name rather than by relying on its hidden attribute.

### Delete `init.ps1` inline, keep the detached process only as a fallback

- **Choice**: `Remove-Item` on itself, with the `-EncodedCommand` trampoline behind a `catch`
- **Alternatives considered**: keep the trampoline as the only path with escaping fixed
- **Reasoning**: PowerShell parses the whole script before executing it, so a script can delete itself, which is what `init.sh` already does. That removes the base64-encoded detached process from the normal path, where it was both fragile and the kind of thing endpoint protection flags. Verified that execution continues normally after self-deletion.

### CI runs a real initialization rather than only linting

- **Choice**: clone into a temp directory and run the script end to end with hostile inputs, asserting on the result
- **Alternatives considered**: PSScriptAnalyzer only
- **Reasoning**: the analyzer reports zero errors on both the old and the new script. It would not have caught a single one of these bugs. The smoke test uses `-Name MyProjectApi` (the double-replace trap) and a password containing `$$` and `$&`, and was validated in both directions: it fails 8 assertions against the old script and passes all of them against the new one.

## Verification

- `init.ps1` parses cleanly; PSScriptAnalyzer reports 0 errors (remaining warnings are `PSAvoidUsingWriteHost`, which is deliberate for a coloured interactive installer, and `PSUseSingularNouns`).
- Full initialization run against a scratch clone with `-Name MyProjectApi -Port 14000 -Password 'Pa$$w0rd$&x!' -Email 'dev$1@test.com'`: no placeholders left, credentials written verbatim, all directories renamed to `MyProjectApi.*`, all 5 UTF-8 BOMs preserved, working tree clean, both init scripts removed.
- The same inputs against the previous `init.ps1` produce `myprojectapiApi.*` directories and 4 unsubstituted placeholders, confirming the regressions were real.
- Edge cases exercised: no `.git` at all, repo under a directory named `bin`, repo path containing an apostrophe, redirected stdin, non-numeric and out-of-range ports, lowercase and missing `-Name`, and the `Myproject` case variant.
- `init.sh` re-run after its one-line change: unchanged behaviour, template cleanup still correct.
- Locale check: `ConvertTo-KebabCase "InvoiceHub"` returns `invoice-hub` under `en-US`, `tr-TR` and `az-Latn-AZ`.

## Follow-ups

- [ ] The `smoke` job assumes a running Docker daemon on GitHub-hosted runners because the prerequisite check requires one. This is the one part that could not be verified locally; if `windows-latest` turns out not to satisfy it, either relax the Docker check to the steps that actually need it or install a daemon in the job.
- [ ] `init.sh` shares three defects that are harmless on POSIX but were fixed only on the PowerShell side: `--yes` with an out-of-range `--port` loops forever, `--yes` with an invalid `--name` re-prompts with no reader, and a failed build does not set a non-zero exit. Worth aligning so the two scripts do not drift again.
- [ ] Generated JWT signing key, encryption key and superuser password are committed in plaintext by both scripts (`appsettings.json`, `appsettings.Development.json`) and stay in history. Pre-existing and unchanged here, but it deserves its own decision.
- [ ] `init-verify` runs `./init.sh` only. Now that CI covers both, consider teaching the skill to pick the script matching the host.
