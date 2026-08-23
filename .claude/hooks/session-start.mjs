#!/usr/bin/env node
// SessionStart hook: checks development environment prerequisites

import { execSync } from 'child_process';
import { resolve } from 'path';

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();

function run(cmd, opts = {}) {
  return execSync(cmd, {
    encoding: 'utf8',
    stdio: ['pipe', 'pipe', 'pipe'],
    timeout: 5000,
    ...opts,
  }).trim();
}

console.log('=== NETrock Environment Check ===');

// .NET SDK
try {
  console.log(run('dotnet --version'));
  console.log('OK .NET SDK');
} catch {
  console.log('MISSING .NET SDK');
}

// pnpm
try {
  console.log(run('pnpm --version', { cwd: resolve(projectDir, 'src/frontend') }));
  console.log('OK pnpm');
} catch {
  console.log('MISSING pnpm');
}

// dotnet-ef (local tool manifest at .config/dotnet-tools.json)
try {
  const tools = run('dotnet tool list', { cwd: projectDir });
  if (tools.includes('dotnet-ef')) {
    console.log('OK dotnet-ef tool');
  } else {
    console.log('MISSING dotnet-ef (run: dotnet tool restore)');
  }
} catch {
  console.log('MISSING dotnet-ef (run: dotnet tool restore)');
}

// Docker
try {
  run('docker info');
  console.log('OK Docker');
} catch {
  console.log('MISSING Docker');
}

console.log('=== Ready ===');
