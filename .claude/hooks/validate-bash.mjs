#!/usr/bin/env node
// PreToolUse hook: blocks dangerous bash commands
// Exit code 2 = block, 0 = allow

import { readFileSync } from 'fs';

let input;
try {
  input = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const command = input?.tool_input?.command;
if (!command) process.exit(0);

const blocks = [
  {
    pattern: /(^|[;&|])\s*npm\s+install\b/,
    msg: 'Use pnpm, not npm. Run: pnpm install',
  },
  {
    pattern: /git\s+push\s+.*--force(?!-)/,
    msg: 'Force push blocked. Use --force-with-lease if you must, or ask the user first.',
  },
  {
    pattern: /rm\s+-rf\s+(\/|~|\$HOME|\.\.)/,
    msg: 'Destructive rm -rf on sensitive path blocked.',
  },
  {
    pattern: /git\s+reset\s+--hard\s*$/m,
    msg: "Bare 'git reset --hard' blocked - specify a target commit.",
  },
  {
    pattern: /git\s+clean\s+-[a-zA-Z]*f/,
    msg: 'git clean blocked - this removes untracked files permanently.',
  },
  {
    pattern: /git\s+(checkout|restore)\s+(--\s+)?\.\s*$/m,
    msg: 'Discarding all changes blocked. Specify individual files.',
  },
  {
    pattern: /git\s+branch\s+-D\b/,
    msg: 'Force branch deletion blocked. Use -d (safe delete) or ask the user.',
  },
  {
    pattern: /(curl|wget)\s+.*\|\s*(ba)?sh/,
    msg: 'Piping remote scripts to shell blocked. Download and review first.',
  },
  {
    pattern: /git\s+push\s+\S+\s+:(?!-)/,
    msg: 'Remote branch deletion blocked. Ask the user first.',
  },
  {
    pattern: /git\s+push\s+.*--delete\b/,
    msg: 'Remote branch deletion blocked. Ask the user first.',
  },
];

for (const { pattern, msg } of blocks) {
  if (pattern.test(command)) {
    process.stderr.write(msg + '\n');
    process.exit(2);
  }
}

process.exit(0);
