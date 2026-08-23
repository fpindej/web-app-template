#!/usr/bin/env node
// PostToolUse hook: auto-formats files after Write|Edit operations

import { readFileSync, existsSync, readdirSync } from 'fs';
import { execFileSync } from 'child_process';
import { resolve, extname } from 'path';

let input;
try {
  input = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const filePath = input?.tool_input?.file_path;
if (!filePath || !existsSync(filePath)) process.exit(0);

const projectDir = process.env.CLAUDE_PROJECT_DIR;
if (!projectDir) process.exit(0);

const ext = extname(filePath);
const backendDir = resolve(projectDir, 'src/backend');
const frontendDir = resolve(projectDir, 'src/frontend');
const inDir = (dir) => resolve(filePath).startsWith(dir + '/');

try {
  if (ext === '.cs' && inDir(backendDir)) {
    const slnx = readdirSync(backendDir).find((f) => f.endsWith('.slnx'));
    if (slnx) {
      execFileSync(
        'dotnet',
        ['format', resolve(backendDir, slnx), '--include', filePath, '--no-restore'],
        { stdio: 'ignore' },
      );
    }
  } else if (
    ['.ts', '.svelte', '.js', '.json', '.css', '.html'].includes(ext) &&
    inDir(frontendDir)
  ) {
    const prettierBin = resolve(frontendDir, 'node_modules/.bin/prettier');
    if (existsSync(prettierBin)) {
      execFileSync(prettierBin, ['--write', filePath], {
        cwd: frontendDir,
        stdio: 'ignore',
      });
    }
  }
} catch {
  // Formatting is best-effort
}

process.exit(0);
