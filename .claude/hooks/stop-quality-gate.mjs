#!/usr/bin/env node
// Stop hook: blocks Claude from finishing with uncommitted src/ changes.
// A per-session marker file ensures we block at most once per dirty-file set,
// so this can never loop even if the work is intentionally left uncommitted.

import { readFileSync, writeFileSync } from 'fs';
import { execSync } from 'child_process';
import { createHash } from 'crypto';
import { tmpdir } from 'os';
import { join } from 'path';

const projectDir = process.env.CLAUDE_PROJECT_DIR;
if (!projectDir) process.exit(0);

let input = {};
try {
  input = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  // No input - fall through with defaults
}

const git = (cmd) =>
  execSync(cmd, {
    cwd: projectDir,
    encoding: 'utf8',
    stdio: ['pipe', 'pipe', 'pipe'],
    timeout: 5000,
  });

let branchWarning = '';
let dirtyFiles = [];

try {
  const branch = git('git rev-parse --abbrev-ref HEAD').trim();
  if (branch === 'main' || branch === 'master') {
    branchWarning = `On ${branch} branch - create a feature branch before committing.`;
  }

  const staged = git('git diff --cached --name-only').trim();
  const unstaged = git('git diff --name-only').trim();
  const untracked = git('git ls-files --others --exclude-standard -- src/').trim();
  dirtyFiles = [
    ...new Set(
      [...staged.split('\n'), ...unstaged.split('\n'), ...untracked.split('\n')].filter(Boolean),
    ),
  ].sort();
} catch {
  process.exit(0);
}

if (dirtyFiles.length === 0) {
  if (branchWarning) {
    console.log(JSON.stringify({ systemMessage: `Quality gate: ${branchWarning}` }));
  }
  process.exit(0);
}

// Block only once per (session, dirty-file set): if we already blocked for this
// exact set, Claude has decided not to commit - let it stop with a visible note.
const setHash = createHash('sha256').update(dirtyFiles.join('\n')).digest('hex').slice(0, 16);
const sessionId =
  String(input.session_id || 'unknown').replace(/[^A-Za-z0-9-]/g, '').slice(0, 64) || 'unknown';
const marker = join(tmpdir(), `claude-stop-gate-${sessionId}`);
let alreadyBlocked = input.stop_hook_active === true;
try {
  if (readFileSync(marker, 'utf8') === setHash) alreadyBlocked = true;
} catch {
  // No marker yet
}

if (!alreadyBlocked) {
  try {
    writeFileSync(marker, setHash);
  } catch {
    // Best effort - without a marker we still have stop_hook_active as fallback
  }
  const list = dirtyFiles.slice(0, 10).join(', ');
  const more = dirtyFiles.length > 10 ? ` (+${dirtyFiles.length - 10} more)` : '';
  console.log(
    JSON.stringify({
      decision: 'block',
      reason:
        `Uncommitted changes: ${list}${more}. ` +
        `If this is a logically complete unit of work, commit it now (Conventional Commit). ` +
        `${branchWarning} ` +
        `If the work is intentionally unfinished or not yours to commit, you may stop - but tell the user why it is uncommitted.`,
    }),
  );
  process.exit(0);
}

console.log(
  JSON.stringify({
    systemMessage: `Quality gate: ${dirtyFiles.length} file(s) still uncommitted. ${branchWarning}`.trim(),
  }),
);
process.exit(0);
