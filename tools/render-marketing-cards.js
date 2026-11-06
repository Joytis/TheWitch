#!/usr/bin/env node
/*
 * render-marketing-cards.js — Workshop gallery images: framed card grids on a themed background.
 *
 *   node tools/render-marketing-cards.js [--character witch] [--scale 1.25] [--reroll]
 *
 * Cards are drawn with the Frame Previewer renderer (pages/frame-preview/renderer.js, the canvas port
 * of card.tscn) inside headless Chrome/Edge, so the frames, fonts and colours match the game.
 *
 * WHICH cards: <workshop>/marketing/card-grids.json is the lock file. On first run (or --reroll) it is
 * written from a deterministic sample: per rarity, the cards sorted by sha256("<seed>:<ENTRY>"), first
 * N taken; the starter grid is the unique cards of the character's StartingDeck in deck order. After
 * that the lock is the source of truth, so new cards never reshuffle the images. Edit it by hand to
 * swap or reorder cards (grid order = list order).
 *
 * Output: one image per grid, written next to the lock (<workshop>/marketing/<file>). The file's extension
 * picks the encoding: .jpg/.jpeg = JPEG (quality JPEG_QUALITY), else PNG. The grids ship as JPEG because the
 * PNGs run ~4.5 MB each and Steam rejected the gallery update with k_EResultLimitExceeded. Upload order is
 * the marketing.json manifest's business, not this script's.
 */
'use strict';
const fs = require('fs');
const path = require('path');
const os = require('os');
const crypto = require('crypto');
const { execFileSync } = require('child_process');
const { select } = require('../Docs/card-data/characters.js');

const ROOT = path.resolve(__dirname, '..');

// Per-character marketing setup. Colours are the character's own (Witch.cs Color / DarkColor family).
const MARKETING = {
  witch: {
    workshop: 'TheWitch/workshop',
    characterFile: 'TheWitch/TheWitchCode/Character/Witch.cs',
    keywordsFile: 'TheWitch/TheWitchCode/Cards/WitchKeywords.cs',
    keywordLoc: 'TheWitch/TheWitch/localization/eng/card_keywords.json',
    frameStyle: 'witch',
    seed: 'thewitch-marketing-v1',
    theme: {
      top: '#3b1b1a', bottom: '#170908',      // darkened rose brown (Witch.Color 5E2626 / DarkColor 3D1714)
      glow: 'rgba(110,46,44,0.75)',           // rose-brown bloom behind the cards
      starterAccent: '#d39a8c',               // starter grid has no rarity colour: warm rose
    },
    grids: [
      { file: 'cards-common.jpg', rarity: 'Common', count: 15, cols: 5 },
      { file: 'cards-uncommon.jpg', rarity: 'Uncommon', count: 15, cols: 5 },
      { file: 'cards-rare.jpg', rarity: 'Rare', count: 15, cols: 5 },
      { file: 'cards-starter.jpg', rarity: 'Starter', cols: 4 },
    ],
  },
};

// Base-game keyword placement (CardKeywordOrder) + English titles (card_keywords.json).
const KEYWORDS_BEFORE = ['Ethereal', 'Sly', 'Retain', 'Innate', 'Unplayable'];
const KEYWORDS_AFTER = ['Exhaust', 'Eternal'];
const FRAME_RARITY = { Starter: 'common', Basic: 'common', Common: 'common', Uncommon: 'uncommon', Rare: 'rare' };

// ---- args -------------------------------------------------------------------------------------
const argv = process.argv.slice(2);
const arg = (name, def) => { const i = argv.indexOf(name); return i >= 0 && argv[i + 1] ? argv[i + 1] : def; };
const ch = select(arg('--character', 'witch'))[0];
const mk = MARKETING[ch.key];
if (!mk) throw new Error(`no marketing setup for character "${ch.key}" (add one to MARKETING in ${path.basename(__filename)})`);
const SCALE = Number(arg('--scale', '1.25'));
const REROLL = argv.includes('--reroll');
const JPEG_QUALITY = 0.9;

const rel = p => path.join(ROOT, p);
const readJson = p => JSON.parse(fs.readFileSync(rel(p), 'utf8'));
const cards = readJson(ch.dataFile).cards;
const loc = readJson(ch.loc);
const byEntry = new Map(cards.map(c => [c.entry, c]));
const marketingDir = rel(path.join(mk.workshop, 'marketing'));
const lockPath = path.join(marketingDir, 'card-grids.json');

// ---- selection (lock file) --------------------------------------------------------------------
function starterEntries() {
  const src = fs.readFileSync(rel(mk.characterFile), 'utf8');
  const deck = src.match(/StartingDeck\s*=>\s*\[([\s\S]*?)\];/);
  if (!deck) throw new Error(`no StartingDeck in ${mk.characterFile}`);
  const byClass = new Map(cards.map(c => [path.basename(c.file, '.cs'), c.entry]));
  const out = [];
  for (const [, cls] of deck[1].matchAll(/ModelDb\.Card<(\w+)>\(\)/g)) {
    const entry = byClass.get(cls);
    if (!entry) throw new Error(`starting-deck card ${cls} not in ${ch.dataFile}`);
    if (!out.includes(entry)) out.push(entry);
  }
  return out;
}
function sample(rarity, count) {
  const pool = cards.filter(c => c.rarity === rarity);
  if (pool.length < count) throw new Error(`only ${pool.length} ${rarity} cards, need ${count}`);
  const key = e => crypto.createHash('sha256').update(`${mk.seed}:${e}`).digest('hex');
  return pool.map(c => c.entry).sort((a, b) => key(a).localeCompare(key(b))).slice(0, count);
}
function loadLock() {
  if (fs.existsSync(lockPath) && !REROLL) return JSON.parse(fs.readFileSync(lockPath, 'utf8'));
  const lock = {
    _about: 'Which cards each marketing grid shows, in grid order. Written once by tools/render-marketing-cards.js ' +
      '(deterministic sample, seed below; --reroll rewrites it), then authoritative: edit by hand to swap or reorder.',
    seed: mk.seed,
    grids: mk.grids.map(g => ({
      file: g.file, rarity: g.rarity, cols: g.cols, ...(g.aspect ? { aspect: g.aspect } : {}),
      cards: g.rarity === 'Starter' ? starterEntries() : sample(g.rarity, g.count),
    })),
  };
  fs.mkdirSync(marketingDir, { recursive: true });
  fs.writeFileSync(lockPath, JSON.stringify(lock, null, 2) + '\n');
  console.log(`${REROLL ? 'Rewrote' : 'Wrote'} selection lock ${path.relative(ROOT, lockPath)}`);
  return lock;
}

// ---- card -> renderer opts --------------------------------------------------------------------
function customKeywords() {
  const out = {};
  if (!mk.keywordsFile || !fs.existsSync(rel(mk.keywordsFile))) return out;
  const src = fs.readFileSync(rel(mk.keywordsFile), 'utf8');
  const kwLoc = fs.existsSync(rel(mk.keywordLoc)) ? readJson(mk.keywordLoc) : {};
  for (const m of src.matchAll(/(\[KeywordProperties\(AutoKeywordPosition\.(\w+)\)\][\s\S]*?)?public static CardKeyword (\w+);/g)) {
    const field = m[3];
    const locKey = ch.prefix + field.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toUpperCase() + '.title';
    out[field] = { title: kwLoc[locKey] || field, position: (m[2] || 'None').toLowerCase() };
  }
  return out;
}
const CUSTOM_KW = customKeywords();

// CanonicalKeywords from the card source. A ternary (`Witch.Turbo ? [] : [...]`) uses the default branch.
function keywordLines(card) {
  const src = fs.readFileSync(rel(card.file), 'utf8');
  const m = src.match(/CanonicalKeywords\s*=>\s*([\s\S]*?);/);
  if (!m) return { before: [], after: [] };
  let expr = m[1];
  if (expr.includes('?')) expr = expr.slice(expr.lastIndexOf(':') + 1);
  const base = [...expr.matchAll(/CardKeyword\.(\w+)/g)].map(x => x[1]);
  const custom = [...expr.matchAll(/\w+Keywords\.(\w+)/g)].map(x => x[1]).filter(k => CUSTOM_KW[k]);
  const line = t => `[gold]${t}[/gold].`;
  // GetDescriptionForPile inserts each before-keyword at index 0, so they end up in reverse order.
  const before = KEYWORDS_BEFORE.filter(k => base.includes(k)).reverse().map(line);
  for (const k of custom) if (CUSTOM_KW[k].position === 'before') before.unshift(line(CUSTOM_KW[k].title));
  const after = KEYWORDS_AFTER.filter(k => base.includes(k)).map(line);
  for (const k of custom) if (CUSTOM_KW[k].position === 'after') after.push(line(CUSTOM_KW[k].title));
  return { before, after };
}

function portraitPath(entry) {
  const base = entry.toLowerCase() + '.png';
  for (const dir of [ch.portraits, ...ch.portraitSubdirs.map(d => `${ch.portraits}/${d}`)]) {
    if (fs.existsSync(rel(`${dir}/${base}`))) return rel(`${dir}/${base}`);
  }
  return null;
}
function pngSize(file) {
  const b = fs.readFileSync(file);
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) };
}

const warnings = [];
function cardJob(entry) {
  const c = byEntry.get(entry);
  if (!c) throw new Error(`card ${entry} (in ${path.relative(ROOT, lockPath)}) no longer exists in ${ch.dataFile}`);
  const raw = loc[`${ch.prefix}${entry}.description`];
  if (raw == null) warnings.push(`${entry}: no localized description`);
  if (/:cond:/.test(raw || '')) warnings.push(`${entry}: has a {…:cond:…} clause the renderer skips — check its text`);
  const art = portraitPath(entry);
  if (!art) warnings.push(`${entry}: no portrait art`);
  const size = art ? pngSize(art) : null;
  return {
    entry,
    title: loc[`${ch.prefix}${entry}.title`] || c.name,
    type: c.type.toLowerCase(),
    rarity: FRAME_RARITY[c.rarity] || 'common',
    cost: String(c.cost) === '-1' ? '' : String(c.cost),
    raw: raw || '',
    numbers: c.numbers || {},
    keywords: keywordLines(c),
    art: art ? 'data:image/png;base64,' + fs.readFileSync(art).toString('base64') : null,
    // Full-art (taller-than-wide) sources use the packed in-game slice; landscape art is drawn at full
    // detail (same geometry: the packer stretches it to the 250x190 slot either way).
    ingame: !!size && size.h > size.w,
  };
}

// ---- the page (runs in headless Chrome) ---------------------------------------------------------
function pageScript() {
  /* eslint-env browser */
  return async function main(JOB) {
    const FP = window.FramePreview;
    await FP.ready();
    const loadImg = src => new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
    const L = FP.layout;
    const B = L.bounds;                             // 340 x 456 card units incl. shadow + energy orb overhang
    const GAP_X = 10, GAP_Y = 16, MARGIN = 64;

    // Deterministic PRNG for the mote field.
    function rng(seedStr) {
      let a = 0; for (const ch of seedStr) a = (a * 31 + ch.charCodeAt(0)) >>> 0;
      return () => { a = (a + 0x6D2B79F5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
    }
    function rgba(css, a) {
      const c = document.createElement('canvas').getContext('2d');
      c.fillStyle = css; const v = c.fillStyle;   // normalises to #rrggbb or rgb()
      const m = v.startsWith('#') ? [1, 3, 5].map(i => parseInt(v.slice(i, i + 2), 16)) : v.match(/\d+/g).slice(0, 3).map(Number);
      return `rgba(${m[0]},${m[1]},${m[2]},${a})`;
    }

    function background(ctx, W, H, accent, seed) {
      const T = JOB.theme;
      let g = ctx.createLinearGradient(0, 0, 0, H);
      g.addColorStop(0, T.top); g.addColorStop(1, T.bottom);
      ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
      const R = Math.hypot(W, H) / 2;
      g = ctx.createRadialGradient(W / 2, H * 0.45, 0, W / 2, H * 0.45, R * 0.85);
      g.addColorStop(0, T.glow); g.addColorStop(1, 'rgba(0,0,0,0)');
      ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
      // light rarity touch: a faint tinted bloom, drifting motes, a thin inset rule
      g = ctx.createRadialGradient(W / 2, H * 0.45, 0, W / 2, H * 0.45, R * 0.7);
      g.addColorStop(0, rgba(accent, 0.13)); g.addColorStop(1, rgba(accent, 0));
      ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
      const rand = rng(seed);
      ctx.save();
      for (let i = 0; i < 90; i++) {
        const x = rand() * W, y = rand() * H, r = 0.8 + rand() * 2.4;
        ctx.shadowColor = rgba(accent, 0.6); ctx.shadowBlur = r * 4;
        ctx.fillStyle = rgba(accent, 0.1 + rand() * 0.25);
        ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI * 2); ctx.fill();
      }
      ctx.restore();
      g = ctx.createRadialGradient(W / 2, H / 2, R * 0.55, W / 2, H / 2, R * 1.05);   // vignette
      g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.5)');
      ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
      const inset = 22;
      ctx.strokeStyle = rgba(accent, 0.32); ctx.lineWidth = 2;
      ctx.strokeRect(inset, inset, W - inset * 2, H - inset * 2);
      ctx.strokeStyle = rgba(accent, 0.12); ctx.lineWidth = 1;
      ctx.strokeRect(inset + 7, inset + 7, W - (inset + 7) * 2, H - (inset + 7) * 2);
    }

    const out = [];
    for (const grid of JOB.grids) {
      const S = JOB.scale;
      const cols = grid.cols, rows = Math.ceil(grid.cards.length / cols);
      // Margins are measured from what is visible, not the card canvas bounds (which pad unevenly for
      // the shadow): energy orb top-left, banner right, frame bottom.
      const V = { x0: L.energy[0], y0: L.energy[1], x1: L.banner[0] + L.banner[2], y1: L.frame[1] + L.frame[3] };
      const pitchX = B.w + GAP_X, pitchY = B.h + GAP_Y;
      const contentW = (cols - 1) * pitchX + (V.x1 - V.x0), contentH = (rows - 1) * pitchY + (V.y1 - V.y0);
      let W = contentW + 2 * MARGIN, H = contentH + 2 * MARGIN;
      if (grid.aspect) { if (W / H > grid.aspect) H = W / grid.aspect; else W = H * grid.aspect; }
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(W * S); canvas.height = Math.round(H * S);
      const ctx = canvas.getContext('2d');
      ctx.scale(S, S);
      const accent = grid.rarity === 'Starter' ? JOB.theme.starterAccent : FP.swatch('rarity', grid.frameRarity);
      background(ctx, W, H, accent, JOB.seed + ':' + grid.file);

      const x0 = (W - contentW) / 2, y0 = (H - contentH) / 2;
      for (let i = 0; i < grid.cards.length; i++) {
        const c = grid.cards[i];
        const desc = [...c.keywords.before, FP.locToMarkup(c.raw, c.numbers, false), ...c.keywords.after].filter(Boolean).join('\n');
        const card = document.createElement('canvas');
        FP.renderCard(card, {
          style: JOB.frameStyle, type: ['attack', 'skill', 'power'].includes(c.type) ? c.type : 'skill', rarity: c.rarity,
          title: c.title, cost: c.cost, desc, art: c.art ? await loadImg(c.art) : null, ingame: c.ingame, scale: S,
        });
        const col = i % cols, row = Math.floor(i / cols);
        // centre a short last row
        const inRow = row === rows - 1 ? grid.cards.length - row * cols : cols;
        const rowX = x0 + (contentW - ((inRow - 1) * pitchX + (V.x1 - V.x0))) / 2;
        ctx.drawImage(card, rowX + col * pitchX - (V.x0 - B.x), y0 + row * pitchY - (V.y0 - B.y), B.w, B.h);
        if (/\?|\{|\}/.test(desc)) out.push({ warn: `${c.entry}: unresolved text in "${desc}"` });
      }
      const jpeg = /.jpe?g$/i.test(grid.file);
      out.push({ file: grid.file, w: canvas.width, h: canvas.height,
        data: jpeg ? canvas.toDataURL('image/jpeg', JOB.jpegQuality) : canvas.toDataURL('image/png') });
    }
    const pre = document.createElement('pre');
    pre.id = 'result';
    pre.textContent = JSON.stringify(out);
    document.body.appendChild(pre);
  };
}

function findBrowser() {
  const cands = [process.env.CHROME_PATH,
    'C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
    '/usr/bin/google-chrome', '/usr/bin/chromium', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'];
  const hit = cands.find(p => p && fs.existsSync(p));
  if (!hit) throw new Error('no Chrome/Edge found; set CHROME_PATH');
  return hit;
}

// ---- main ---------------------------------------------------------------------------------------
const lock = loadLock();
const job = {
  scale: SCALE, jpegQuality: JPEG_QUALITY, seed: lock.seed, frameStyle: mk.frameStyle, theme: mk.theme,
  grids: lock.grids.map(g => ({ ...g, frameRarity: FRAME_RARITY[g.rarity] || 'common', cards: g.cards.map(cardJob) })),
};
for (const w of warnings) console.warn('[warn] ' + w);

const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'marketing-cards-'));
try {
  const inline = p => fs.readFileSync(rel(p), 'utf8').replace(/<\/script/gi, '<\\/script');
  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"></head><body>
<script>${inline('pages/frame-preview/assets.js')}</script>
<script>${inline('pages/frame-preview/renderer.js')}</script>
<script>(${pageScript().toString()})(${JSON.stringify(job).replace(/<\/script/gi, '<\\/script')}).catch(e => {
  const pre = document.createElement('pre'); pre.id = 'result'; pre.textContent = JSON.stringify([{ error: String(e && e.stack || e) }]); document.body.appendChild(pre);
});</script></body></html>`;
  const page = path.join(tmp, 'render.html');
  fs.writeFileSync(page, html);

  console.log(`Rendering ${job.grids.length} grid(s) at scale ${SCALE} in headless browser...`);
  const dom = execFileSync(findBrowser(), [
    '--headless=new', '--disable-gpu', '--hide-scrollbars', `--user-data-dir=${path.join(tmp, 'profile')}`,
    '--virtual-time-budget=600000', '--dump-dom', 'file:///' + page.replace(/\\/g, '/'),
  ], { maxBuffer: 1 << 30, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });

  const m = dom.match(/<pre id="result">([\s\S]*?)<\/pre>/);
  if (!m) throw new Error('the render page produced no result (timed out or crashed)');
  const decode = s => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&amp;/g, '&');
  let failed = false;
  for (const r of JSON.parse(decode(m[1]))) {
    if (r.error) { console.error('[error] ' + r.error); failed = true; continue; }
    if (r.warn) { console.warn('[warn] ' + r.warn); continue; }
    const file = path.join(marketingDir, r.file);
    const buf = Buffer.from(r.data.split(',')[1], 'base64');
    fs.writeFileSync(file, buf);
    console.log(`  ${path.relative(ROOT, file)}  ${r.w}x${r.h}  ${(buf.length / 1048576).toFixed(2)} MB`);
  }
  if (failed) process.exitCode = 1;
} finally {
  fs.rmSync(tmp, { recursive: true, force: true });
}
