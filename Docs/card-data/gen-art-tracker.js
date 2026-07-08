#!/usr/bin/env node
// Build the Card Art Tracker CSV from cards.json, preserving artist-edited
// columns (Art Status, Art Brief) from an export of the live Google Sheet.
//
// Usage:
//   node Docs/card-data/gen-art-tracker.js <out.csv> [current-sheet.csv]
//
// - <out.csv>            where to write the merged tracker CSV
// - [current-sheet.csv]  optional: the live sheet exported as CSV. When given,
//   each card's "Art Status" and "Art Brief (artist notes)" are carried over
//   (matched by Card Name). Cards no longer in cards.json are reported as
//   removed; brand-new cards are reported as added.
//
// Art Status logic: artFinal=true in cards.json always wins ("Final");
// otherwise the live sheet's status is kept (artists may use intermediate
// states like "In Progress"); otherwise "Placeholder".

const fs = require('fs');
const path = require('path');

const [outPath, currentPath] = process.argv.slice(2);
if (!outPath) {
  console.error('usage: node gen-art-tracker.js <out.csv> [current-sheet.csv]');
  process.exit(1);
}

const data = require(path.join(__dirname, 'cards.json'));

// --- tiny CSV parse (quoted fields, embedded newlines) ---
function parseCsv(text) {
  const rows = [];
  let row = [], field = '', inQ = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQ) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; }
        else inQ = false;
      } else field += c;
    } else if (c === '"') inQ = true;
    else if (c === ',') { row.push(field); field = ''; }
    else if (c === '\n' || c === '\r') {
      if (c === '\r' && text[i + 1] === '\n') i++;
      row.push(field); field = '';
      if (row.length > 1 || row[0] !== '') rows.push(row);
      row = [];
    } else field += c;
  }
  if (field !== '' || row.length) { row.push(field); rows.push(row); }
  return rows;
}

// --- load live sheet edits keyed by card name ---
const live = new Map();
if (currentPath) {
  const rows = parseCsv(fs.readFileSync(currentPath, 'utf8'));
  const header = rows[0].map(h => h.trim());
  const col = name => header.findIndex(h => h.toLowerCase().startsWith(name));
  const iName = col('card name'), iStatus = col('art status'), iBrief = col('art brief');
  if (iName === -1) { console.error('current-sheet.csv: no "Card Name" column'); process.exit(1); }
  for (const r of rows.slice(1)) {
    live.set(r[iName], {
      status: iStatus !== -1 ? r[iStatus] : '',
      brief: iBrief !== -1 ? r[iBrief] : '',
    });
  }
}

// --- sanitize card text for artists (strip loc tokens / code refs) ---
function clean(s) {
  return String(s ?? '')
    .replace(/\{(\w+):diff\(\)\}/g, (_, v) => varValue(v))       // {X:diff()} → number if known
    .replace(/\{InCombat:[^}]*\|\}/g, '')                         // in-combat-only clauses
    .replace(/\{(\w+)\}/g, (_, v) => varValue(v))                 // bare {X}
    .replace(/<see cref=""?(\w+)""? \/>/g, '$1')
    .replace(/<\/?c>/g, '')
    .replace(/\s+/g, ' ')
    .trim();
  function varValue(v) { return cur && cur.numbers && cur.numbers[v] != null ? String(cur.numbers[v]) : v; }
}
let cur = null;

const esc = s => '"' + String(s ?? '').replace(/"/g, '""') + '"';
const header = ['Art Status', 'Card Name', 'Type', 'Rarity', 'Cost', 'Card Text',
  'Upgrade', 'Mechanics', 'Design Notes', 'Art Brief (artist notes)', 'Placeholder Art Path'];
const out = [header.map(esc).join(',')];

const added = [], seen = new Set();
for (const c of data.cards) {
  cur = c;
  seen.add(c.name);
  const prev = live.get(c.name);
  if (currentPath && !prev) added.push(c.name);
  const status = c.artFinal ? 'Final' : (prev && prev.status ? prev.status : 'Placeholder');
  const brief = prev ? prev.brief : '';
  const art = 'TheWicken/images/card_portraits/big/' + c.entry.toLowerCase() + '.png';
  out.push([
    status, c.name, c.type, c.rarity, c.cost === -1 ? 'X' : c.cost,
    clean(c.text), clean(c.upgrade), (c.mechanics || []).join(', '),
    clean(c.note), brief, art,
  ].map(esc).join(','));
}

fs.writeFileSync(outPath, out.join('\n'));

const removed = [...live.keys()].filter(n => n && !seen.has(n));
console.log(`${data.cards.length} cards written to ${outPath}`);
if (currentPath) {
  console.log(`preserved artist columns for ${live.size} live rows`);
  if (added.length) console.log('ADDED: ' + added.join(', '));
  if (removed.length) console.log('REMOVED (were in sheet, gone from cards.json): ' + removed.join(', '));
  if (!added.length && !removed.length) console.log('no cards added/removed');
}
