#!/usr/bin/env node
// Generate pages/art-tracker.html — the static, self-contained art tracker served by GitHub Pages.
// One page, sectioned per character (../card-data/characters.js):
//   - <dataFile>   (Docs/card-data/cards.json | augur.json — cards; artFinal=true → Final)
//   - <briefs>     (Docs/art-tracker/card-briefs.json | augur-briefs.json — artist / brief / stage per card)
//   - <assets>     (Docs/art-tracker/assets.json | augur-assets.json — non-card asset categories)
// The page itself is tracker.template.html (data injected at /*@@DATA@@*/); cards render in their real
// frames through the Frame Previewer's renderer (pages/frame-preview/).
//
// Usage: node Docs/art-tracker/regen-art-tracker.js

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { KEYS, CHARACTERS } = require(path.join(__dirname, '..', 'card-data', 'characters.js'));

const root = path.join(__dirname, '..', '..');
const CARD_DIMS = '1000x760 or 2000x1520';
// Card art must be delivered at exactly one of these sizes (health check 'Non-standard resolution').
const CARD_SIZES = [[1000, 760], [2000, 1520]];
const FULL_ART_DIMS = '~600x842 (full-art: taller than wide)';   // Ancient frame; packs to 250x351
// .replace strips a UTF-8 BOM — editors re-save the localization JSON with one on and off.
const readJson = (p) => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^﻿/, ''));
const readJsonOr = (p, fallback) => (fs.existsSync(p) ? readJson(p) : fallback);

// Status: Final (artFinal / done) > explicit stage > artist assigned > unassigned.
const STAGES = { assigned: 'Assigned', sketching: 'Sketching' };
function derive(done, artist, stage) {
  if (done) return 'Final';
  if (stage && STAGES[stage]) return STAGES[stage];
  return artist ? 'Assigned' : 'Unassigned';
}

// ---- source image facts (dimensions from the PNG header; placeholder = card.png or a shared copy) -----
function pngSize(file) {
  try {
    const fd = fs.openSync(file, 'r'), b = Buffer.alloc(24);
    fs.readSync(fd, b, 0, 24, 0); fs.closeSync(fd);
    return b.toString('ascii', 12, 16) === 'IHDR' ? { w: b.readUInt32BE(16), h: b.readUInt32BE(20) } : null;
  } catch { return null; }
}
const sha1 = (file) => { try { return crypto.createHash('sha1').update(fs.readFileSync(file)).digest('hex'); } catch { return null; } };
function imgFacts(p, placeholderHashes) {
  const file = p && path.join(root, p);
  if (!file || !fs.existsSync(file)) return { hasArt: false, srcW: 0, srcH: 0, placeholderArt: false };
  const size = pngSize(file) || { w: 0, h: 0 };
  return { hasArt: true, srcW: size.w, srcH: size.h, placeholderArt: !!placeholderHashes && placeholderHashes.has(sha1(file)) };
}
// Card art that is the generic card.png back, or byte-identical across several cards, is a placeholder
// (same rule as the card-designs server's "No Art" state).
function placeholderHashes(ch, artPaths) {
  const out = new Set(), seen = new Map();
  const back = sha1(path.join(root, ch.portraits, 'card.png'));
  if (back) out.add(back);
  for (const p of new Set(artPaths)) {
    const h = sha1(path.join(root, p));
    if (!h) continue;
    if (seen.has(h)) out.add(h); else seen.set(h, p);
  }
  return out;
}

// sanitize card text (plain, for search + table view)
let cur = null;
function clean(s) {
  return String(s ?? '')
    .replace(/\{(\w+):diff\(\)\}/g, (_, v) => varValue(v))
    .replace(/\{InCombat:[^}]*\|\}/g, '')
    .replace(/\{(\w+)\}/g, (_, v) => varValue(v))
    .replace(/<see cref=""?(\w+)""? \/>/g, '$1')
    .replace(/<\/?c>/g, '')
    .replace(/\s+/g, ' ')
    .trim();
  function varValue(v) { return cur && cur.numbers && cur.numbers[v] != null ? String(cur.numbers[v]) : v; }
}

// ---- in-game artist credit (ArtBy) -------------------------------------------------------------
// Characters with an Artists/Artist.cs credit their art in game; read each card's ArtBy override (or the
// base-class default) so the health panel can flag Final art whose credit disagrees with the tracker.
function creditReader(ch) {
  const codeDir = path.dirname(path.join(root, ch.cardsDir));
  const artistFile = path.join(codeDir, 'Artists', 'Artist.cs');
  if (!fs.existsSync(artistFile)) return null;
  const names = {};
  for (const m of fs.readFileSync(artistFile, 'utf8').matchAll(/static readonly Artist (\w+)\s*=\s*new\("([^"]+)"/g)) names[m[1]] = m[2];
  const ARTBY = /ArtBy\s*=>\s*(?:Artists\.)?(?:Artist\.)?(\w+)\s*;/;
  let fallback = null;
  for (const f of fs.readdirSync(path.join(root, ch.cardsDir))) {
    if (!f.endsWith('.cs')) continue;
    const src = fs.readFileSync(path.join(root, ch.cardsDir, f), 'utf8');
    const m = /virtual\s+Artists\.Artist\?\s+ArtBy/.test(src) && src.match(ARTBY);
    if (m) { fallback = m[1]; break; }
  }
  return (file) => {
    let key = fallback;
    try { const m = fs.readFileSync(path.join(root, file), 'utf8').match(ARTBY); if (m) key = m[1]; } catch {}
    return key === 'null' || !key ? '' : (names[key] || key);
  };
}

const sameArtist = (a, b) => String(a).toLowerCase().replace(/[^a-z0-9]/g, '') === String(b).toLowerCase().replace(/[^a-z0-9]/g, '');

// Build one character's section: card rows + asset categories (Powers auto-enumerated from its loc).
function buildCharacter(ch) {
  const cardsData = readJsonOr(path.join(root, ch.dataFile), { cards: [] });
  const briefs = readJsonOr(path.join(root, ch.briefs), {}).briefs || {};
  const assetsFile = readJsonOr(path.join(root, ch.assets), {});
  const assetCats = JSON.parse(JSON.stringify(assetsFile.categories || []));
  const cardLoc = readJsonOr(path.join(root, ch.loc), {});
  const credit = creditReader(ch);

  // Source art lives at <portraits>/<entry>.png; some characters author subsets under a subdir
  // (Witch familiar tokens → familiar/). Probe root then each subdir; fall back to the root path.
  function cardArtPath(entry) {
    const base = entry.toLowerCase() + '.png';
    for (const dir of [ch.portraits + '/', ...ch.portraitSubdirs.map((d) => `${ch.portraits}/${d}/`)]) {
      if (fs.existsSync(path.join(root, dir + base))) return dir + base;
    }
    return ch.portraits + '/' + base; // convention target even if missing
  }
  // Frame the card renders in (pages/frame-preview styles).
  const styleOf = (c) => ch.key === 'augur' ? 'augur'
    : c.rarity === 'Status' ? 'status'
    : /[\\/]Familiar[\\/]/.test(c.file || '') ? 'familiar' : 'witch';

  const cardPaths = (cardsData.cards || []).map(c => cardArtPath(c.entry));
  const placeholders = placeholderHashes(ch, cardPaths);
  const cardRows = (cardsData.cards || []).map(c => {
    cur = c;
    const b = briefs[c.name] || {};
    const p = cardArtPath(c.entry);
    return {
      kind: 'card',
      name: c.name,
      entry: c.entry,
      artist: b.artist || '',
      stage: b.stage || '',
      final: !!c.artFinal,
      status: derive(c.artFinal, b.artist, b.stage),
      brief: b.brief || '',
      credit: credit ? credit(c.file) : null,
      type: c.type,
      rarity: c.rarity,
      cost: c.cost === -1 || c.cost === '-1' ? '' : String(c.cost),
      text: clean(c.text),
      desc: cardLoc[`${ch.prefix}${c.entry}.description`] || '',
      numbers: c.numbers || {},
      upgrade: clean(c.upgrade),
      mechanics: (c.mechanics || []).filter(m => m && m !== 'None'),
      role: c.role || [],
      style: styleOf(c),
      path: p,
      dims: c.rarity === 'Ancient' ? FULL_ART_DIMS : CARD_DIMS,
      ...imgFacts(p, placeholders),
    };
  });

  // Powers tab: auto-enumerated from the localization file (every power must have loc, so it is the
  // authoritative list — new powers appear on the next regen). Optional per-power curation (artist/done/brief/stage)
  // lives in the assets file under "powerOverrides", keyed by the loc entry (e.g. "HEX_POWER").
  const powerLoc = readJsonOr(path.join(root, ch.powersLoc), {});
  const powerOverrides = assetsFile.powerOverrides || {};
  const imagesDir = ch.portraits.replace(/\/card_portraits$/, '');
  const powerEntries = Object.keys(powerLoc)
    .filter(k => k.startsWith(ch.prefix) && k.endsWith('.title'))
    .map(k => k.slice(ch.prefix.length, -'.title'.length));
  const powersCat = {
    id: 'powers',
    title: 'Powers',
    dims: '256x256',
    assets: powerEntries.map(entry => {
      const o = powerOverrides[entry] || {};
      cur = null;
      return {
        name: powerLoc[`${ch.prefix}${entry}.title`] + ` (${entry.toLowerCase()})`,
        ref: { power: entry },
        artist: o.artist || '', done: !!o.done, brief: o.brief || '', stage: o.stage || '',
        effect: clean(String(powerLoc[`${ch.prefix}${entry}.description`] || '').replace(/\[\/?[a-z]+\]/g, '')),
        path: imagesDir + '/powers/' + entry.toLowerCase() + '.png',
      };
    }),
  };
  // Powers with mod art but base-game loc (ICustomPower subclasses) can't be enumerated from powers.json —
  // they are listed by hand in the assets file under "extraPowers", keyed by entry.
  for (const [entry, o] of Object.entries(assetsFile.extraPowers || {})) {
    powersCat.assets.push({
      name: (o.name || entry) + ` (${entry.toLowerCase()})`,
      ref: { extraPower: entry },
      artist: o.artist || '', done: !!o.done, brief: o.brief || '', stage: o.stage || '',
      effect: o.effect || '',
      path: imagesDir + '/powers/' + entry.toLowerCase() + '.png',
    });
  }
  const petsIdx = assetCats.findIndex(c => c.id === 'pets');
  if (petsIdx >= 0) assetCats.splice(petsIdx, 0, powersCat); else assetCats.push(powersCat);

  const categories = assetCats.map(c => ({
    id: c.id, title: c.title, dims: c.dims,
    assets: c.assets.map(a => ({
      kind: 'asset',
      name: a.name, artist: a.artist || '', stage: a.stage || '', final: !!a.done, status: derive(a.done, a.artist, a.stage), brief: a.brief || '',
      ref: a.ref || { cat: c.id, name: a.name },  // where edit mode writes it back (Docs/card-data/server.js /api/asset)
      rarity: a.rarity || '', orientation: a.orientation || '',
      effect: a.effect || '', path: a.path || '', dims: a.dims || c.dims || '',
      ...imgFacts(a.path),
    })),
  }));

  // ---- data health ----------------------------------------------------------------------------
  const names = new Set(cardRows.map(r => r.name));
  const shapeOff = (r) => r.srcW && r.srcH && r.srcW >= r.srcH && Math.abs((r.srcW / r.srcH) / (1000 / 760) - 1) > 0.015;
  const health = {
    orphanBriefs: Object.keys(briefs).filter(n => !names.has(n)),
    creditMismatch: cardRows.filter(r => r.status === 'Final' && r.credit !== null && r.artist && !sameArtist(r.artist, r.credit))
      .map(r => ({ name: r.name, entry: r.entry, tracker: r.artist, game: r.credit || '(uncredited)' })),
    finalNoArtist: cardRows.filter(r => r.status === 'Final' && !r.artist).map(r => ({ name: r.name, entry: r.entry, game: r.credit || '' })),
    missingArt: [...cardRows, ...categories.flatMap(c => c.assets)].filter(r => r.path && (!r.hasArt || r.placeholderArt))
      .map(r => ({ name: r.name, entry: r.entry || '', file: r.path.split('/').pop(), placeholder: r.hasArt })),
    offShape: cardRows.filter(r => r.hasArt && !r.placeholderArt && r.rarity !== 'Ancient' && shapeOff(r)).map(r => ({ name: r.name, entry: r.entry, w: r.srcW, h: r.srcH })),
    tallArt: cardRows.filter(r => r.hasArt && r.srcH > r.srcW && r.rarity !== 'Ancient').map(r => ({ name: r.name, entry: r.entry, w: r.srcW, h: r.srcH })),
    nonStandardSize: cardRows.filter(r => r.hasArt && !r.placeholderArt && r.rarity !== 'Ancient' && r.srcW && !CARD_SIZES.some(([w, h]) => r.srcW === w && r.srcH === h))
      .map(r => ({ name: r.name, entry: r.entry, w: r.srcW, h: r.srcH })),
    ancientLandscape: cardRows.filter(r => r.hasArt && !r.placeholderArt && r.rarity === 'Ancient' && r.srcW >= r.srcH).map(r => ({ name: r.name, entry: r.entry, w: r.srcW, h: r.srcH })),
  };

  return {
    key: ch.key,
    label: ch.label,
    generated: cardsData.generated || '',
    cards: cardRows,
    categories,
    health,
  };
}

const data = {
  generated: new Date().toISOString().slice(0, 10),
  stages: STAGES,
  characters: KEYS.map((k) => buildCharacter(CHARACTERS[k])),
};

const template = fs.readFileSync(path.join(__dirname, 'tracker.template.html'), 'utf8');
// `<` escaped so card text can never close the inline <script>.
const html = template.replace('/*@@DATA@@*/null', () => JSON.stringify(data).replace(/</g, '\\u003c'));
const outPath = path.join(root, 'pages', 'art-tracker.html');
fs.writeFileSync(outPath, html);

for (const sec of data.characters) {
  const other = sec.categories.reduce((n, c) => n + c.assets.length, 0);
  console.log(`art-tracker.html [${sec.label}]: ${sec.cards.length} cards + ${other} other assets = ${sec.cards.length + other} rows`);
  const h = sec.health;
  const missing = h.missingArt.filter(m => !m.placeholder);
  if (missing.length) console.log(`MISSING IMAGE FILES [${sec.label}]:\n  ` + missing.map(m => m.name).join('\n  '));
  const issues = { 'orphan briefs': h.orphanBriefs.length, 'credit mismatches': h.creditMismatch.length,
    'off-shape art': h.offShape.length, 'non-standard resolutions': h.nonStandardSize.length, 'tall art': h.tallArt.length, 'landscape art on Ancient cards': h.ancientLandscape.length };
  const list = Object.entries(issues).filter(([, n]) => n).map(([k, n]) => `${n} ${k}`);
  if (list.length) console.log(`DATA HEALTH [${sec.label}]: ${list.join(', ')} (see the tracker's health panel)`);
}
