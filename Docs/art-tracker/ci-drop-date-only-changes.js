#!/usr/bin/env node
// CI helper for .github/workflows/art-tracker.yml: after a regen, revert every changed text file whose
// ONLY difference is a generated-on date stamp, so the bot doesn't commit a no-op refresh on every push.
// Usage: node Docs/art-tracker/ci-drop-date-only-changes.js <path>...   (paths/dirs to consider)

const { execFileSync } = require('child_process');
const fs = require('fs');

const git = (...args) => execFileSync('git', args, { encoding: 'utf8', maxBuffer: 64 << 20 });
const norm = (s) => s.replace(/\d{4}-\d{2}-\d{2}/g, 'DATE');

const changed = git('diff', '--name-only', '--', ...process.argv.slice(2)).split('\n').filter(Boolean);
let reverted = 0;
for (const f of changed) {
  if (!/\.(json|html|md|js)$/.test(f)) continue;
  let before;
  try { before = git('show', `HEAD:${f}`); } catch { continue; }
  if (norm(before) === norm(fs.readFileSync(f, 'utf8'))) { git('checkout', '--', f); reverted++; }
}
console.log(`date-only changes reverted: ${reverted} of ${changed.length} changed file(s)`);
