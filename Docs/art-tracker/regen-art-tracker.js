#!/usr/bin/env node
// Generate pages/art-tracker.html — a static, self-contained art-asset tracker
// suitable for GitHub Pages. One page, sectioned per character (../card-data/characters.js):
//   - <dataFile>   (Docs/card-data/cards.json | augur.json — cards; artFinal=true → Final)
//   - <briefs>     (Docs/art-tracker/card-briefs.json | augur-briefs.json — artist / brief per card)
//   - <assets>     (Docs/art-tracker/assets.json | augur-assets.json — non-card asset categories)
// Thumbnails are referenced by repo-relative paths (../TheWitch/images/...) so the
// page works when the whole repo is served (Pages) or opened from Docs/ locally.
//
// Usage: node Docs/art-tracker/regen-art-tracker.js

const fs = require('fs');
const path = require('path');
const { KEYS, CHARACTERS } = require(path.join(__dirname, '..', 'card-data', 'characters.js'));

const root = path.join(__dirname, '..', '..');
const CARD_DIMS = '1000x760';
// .replace strips a UTF-8 BOM — editors re-save the localization JSON with one on and off.
const readJson = (p) => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const readJsonOr = (p, fallback) => (fs.existsSync(p) ? readJson(p) : fallback);

// sanitize card text (same rules as gen-art-tracker.js)
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

// status is derived: done → Done; artist assigned → In Progress; else Placeholder
function derive(done, artist) { return done ? 'Done' : (artist ? 'In Progress' : 'Placeholder'); }
function exists(p) { return p && fs.existsSync(path.join(root, p)); }

// Build one character's section: card rows + asset categories (Powers auto-enumerated from its loc).
function buildCharacter(ch) {
  const cardsData = readJsonOr(path.join(root, ch.dataFile), { cards: [] });
  const briefs = readJsonOr(path.join(root, ch.briefs), {}).briefs || {};
  const assetsFile = readJsonOr(path.join(root, ch.assets), {});
  const assetCats = assetsFile.categories || [];

  // Source art lives at <portraits>/<entry>.png; some characters author subsets under a subdir
  // (Witch familiar tokens → familiar/). Probe root then each subdir; fall back to the root path.
  function cardArtPath(entry) {
    const base = entry.toLowerCase() + '.png';
    for (const dir of [ch.portraits + '/', ...ch.portraitSubdirs.map((d) => `${ch.portraits}/${d}/`)]) {
      if (fs.existsSync(path.join(root, dir + base))) return dir + base;
    }
    return ch.portraits + '/' + base; // convention target even if missing
  }

  const cardRows = (cardsData.cards || []).map(c => {
    cur = c;
    const b = briefs[c.name] || {};
    return {
      name: c.name,
      artist: b.artist || '',
      status: derive(c.artFinal, b.artist),
      brief: b.brief || '',
      type: c.type,
      rarity: c.rarity,
      cost: c.cost === -1 ? 'X' : String(c.cost),
      text: clean(c.text),
      upgrade: clean(c.upgrade),
      mechanics: (c.mechanics || []).filter(m => m && m !== 'None').join(', '),
      path: cardArtPath(c.entry),
      dims: CARD_DIMS,
    };
  });

  // Powers tab: auto-enumerated from the localization file (every power must have loc, so it is the
  // authoritative list — new powers appear on the next regen). Optional per-power curation (artist/done/brief)
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
    dims: '64x64 (small) + 256x256 (big)',
    assets: powerEntries.map(entry => {
      const o = powerOverrides[entry] || {};
      cur = null;
      return {
        name: powerLoc[`${ch.prefix}${entry}.title`] + ` (${entry.toLowerCase()})`,
        artist: o.artist || '',
        done: !!o.done,
        brief: o.brief || '',
        effect: clean(String(powerLoc[`${ch.prefix}${entry}.description`] || '').replace(/\[\/?[a-z]+\]/g, '')),
        path: imagesDir + '/powers/' + entry.toLowerCase() + '.png',
      };
    }),
  };
  // Powers with mod art but base-game loc (ICustomPower subclasses) can't be enumerated from powers.json —
  // they are listed by hand in the assets file under "extraPowers", keyed by entry.
  const extraPowers = assetsFile.extraPowers || {};
  for (const [entry, o] of Object.entries(extraPowers)) {
    powersCat.assets.push({
      name: (o.name || entry) + ` (${entry.toLowerCase()})`,
      artist: o.artist || '',
      done: !!o.done,
      brief: o.brief || '',
      effect: o.effect || '',
      path: imagesDir + '/powers/' + entry.toLowerCase() + '.png',
    });
  }
  const petsIdx = assetCats.findIndex(c => c.id === 'pets');
  if (petsIdx >= 0) assetCats.splice(petsIdx, 0, powersCat); else assetCats.push(powersCat);

  // check which referenced images actually exist
  for (const r of cardRows) r.hasArt = exists(r.path);
  for (const cat of assetCats) for (const a of cat.assets) a.hasArt = exists(a.path);

  return {
    key: ch.key,
    label: ch.label,
    generated: cardsData.generated || '',
    cards: cardRows,
    categories: assetCats.map(c => ({
      id: c.id, title: c.title, dims: c.dims,
      assets: c.assets.map(a => ({
        name: a.name, artist: a.artist || '', status: derive(a.done, a.artist), brief: a.brief || '',
        rarity: a.rarity || '', orientation: a.orientation || '',
        effect: a.effect || '', path: a.path || '', dims: a.dims || c.dims || '',
        hasArt: a.hasArt,
      })),
    })),
    // raw (pre-map) lists kept for the console report below
    _assetCats: assetCats,
  };
}

const sections = KEYS.map((k) => buildCharacter(CHARACTERS[k]));
const data = {
  generated: new Date().toISOString().slice(0, 10),
  characters: sections.map(({ _assetCats, ...s }) => s),
};

const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Art Tracker — The Witch · The Augur</title>
<style>
  /* Witch-house palette — keep in sync with pages/analytics.html */
  :root{
    --bg:#2e2226; --panel:#382a2c; --panel2:#41302f; --line:#4d3b3a;
    --ink:#e8dcc8; --muted:#9b8577; --gold:#e6c15c; --accent:#b8d48f;
    --ok:#b8d48f; --warn:#e6c15c; --no:#6d5a55;
  }
  *{box-sizing:border-box}
  body{margin:0;background:var(--bg);color:var(--ink);font:14px/1.45 "Segoe UI",system-ui,sans-serif}
  header{padding:18px 22px;border-bottom:1px solid var(--line);background:var(--panel);position:sticky;top:0;z-index:5}
  h1{margin:0 0 4px;font-size:20px;color:var(--gold);letter-spacing:.5px}
  .sub{color:var(--muted);font-size:12px}
  .chars{display:flex;gap:8px;margin-top:10px;flex-wrap:wrap}
  .char{background:var(--panel2);border:1px solid var(--line);color:var(--ink);padding:6px 18px;border-radius:6px;cursor:pointer;font-size:13px;font-weight:700;letter-spacing:.3px;user-select:none}
  .char.on{background:var(--gold);border-color:var(--gold);color:#3a2a10}
  .tabs{display:flex;gap:6px;margin-top:12px;flex-wrap:wrap}
  .tab{background:var(--panel2);border:1px solid var(--line);color:var(--muted);padding:7px 16px;border-radius:8px 8px 0 0;cursor:pointer;font-size:13px;font-weight:600;user-select:none}
  .tab.on{background:var(--accent);border-color:var(--accent);color:#26301a}
  .controls{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-top:10px}
  input[type=search]{background:var(--panel2);border:1px solid var(--line);color:var(--ink);padding:7px 10px;border-radius:6px;min-width:220px;font-size:13px}
  .chip{background:var(--panel2);border:1px solid var(--line);color:var(--muted);padding:6px 11px;border-radius:14px;cursor:pointer;font-size:12px;user-select:none}
  .chip.on{background:var(--accent);border-color:var(--accent);color:#26301a}
  .prog{margin-left:auto;color:var(--muted);font-size:12px;text-align:right}
  .bar{width:180px;height:7px;background:var(--panel2);border-radius:4px;overflow:hidden;margin-top:4px}
  .bar > i{display:block;height:100%;background:var(--ok);width:0}
  main{padding:14px 22px 60px}
  table{width:100%;border-collapse:collapse}
  th,td{text-align:left;padding:8px 10px;border-bottom:1px solid var(--line);vertical-align:top}
  th{background:var(--panel);color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.6px}
  tbody tr:hover{background:var(--panel2)}
  .name{font-weight:600}
  .meta{color:var(--muted);font-size:12px;white-space:nowrap}
  .text{max-width:340px}
  .brief{max-width:420px;color:#c99a90;font-style:italic;font-size:12px}
  .dims{font:11px/1.4 ui-monospace,Consolas,monospace;color:var(--muted)}
  .pathcell{font:10px/1.4 ui-monospace,Consolas,monospace;color:var(--muted);max-width:220px;word-break:break-all}
  .badge{display:inline-block;padding:1px 8px;border-radius:10px;font-size:11px;font-weight:600;white-space:nowrap}
  .s-done{background:#b8d48f33;color:#cfe3a8}
  .s-inprogress{background:#e6c15c33;color:#e6c15c}
  .s-placeholder{background:#6d5a5533;color:#a08a7d}
  .artist{color:var(--accent);font-size:12px;white-space:nowrap}
  .thumb{width:60px}
  .thumb img{width:52px;height:auto;border-radius:4px;border:1px solid var(--line);background:#241a1d;display:block;cursor:zoom-in}
  .thumb .none{width:52px;height:40px;border:1px dashed var(--line);border-radius:4px;display:flex;align-items:center;justify-content:center;color:var(--no);font-size:10px}
  #lightbox{position:fixed;inset:0;background:#000c;display:none;align-items:center;justify-content:center;z-index:50;cursor:zoom-out}
  #lightbox img{max-width:88vw;max-height:88vh;border-radius:8px;border:1px solid var(--line)}
  .catnote{color:var(--muted);font-size:12px;margin:8px 0 12px}
</style>
</head>
<body>
<header>
  <h1 id="h1">Art Tracker</h1>
  <div class="sub">Generated ${data.generated} from repo state · statuses reflect the current project, not artist assignments</div>
  <div class="chars" id="chars"></div>
  <div class="tabs" id="tabs"></div>
  <div class="controls">
    <input type="search" id="q" placeholder="filter by name / text…">
    <span class="chip" data-f="all">All</span>
    <span class="chip" data-f="Placeholder">Placeholder</span>
    <span class="chip" data-f="In Progress">In Progress</span>
    <span class="chip" data-f="Done">Done</span>
    <div class="prog"><span id="progtext"></span><div class="bar"><i id="progbar"></i></div></div>
  </div>
</header>
<main id="main"></main>
<div id="lightbox"><img alt=""></div>
<script>
const DATA = ${JSON.stringify(data)};
const esc = s => String(s??'').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const scls = s => 's-' + s.toLowerCase().replace(/\\s+/g,'');
// top-level switcher: one section per character; each section has its own tabs (cards + asset categories)
let CH = DATA.characters[0], tab = 'cards', filter = 'all', q = '';
const tabsOf = ch => [{id:'cards', title:'Cards'}].concat(ch.categories.map(c=>({id:c.id,title:c.title})));

function rows() {
  if (tab === 'cards') return CH.cards;
  return CH.categories.find(c=>c.id===tab).assets;
}
function buildTabs() {
  document.getElementById('h1').textContent = CH.label + ' — Art Tracker';
  document.querySelectorAll('.char').forEach(c=>c.classList.toggle('on', c.dataset.c===CH.key));
  tabsEl.innerHTML = tabsOf(CH).map(t=>'<span class="tab" data-t="'+t.id+'">'+esc(t.title)+'</span>').join('');
}
function render() {
  document.querySelectorAll('.tab').forEach(t=>t.classList.toggle('on', t.dataset.t===tab));
  document.querySelectorAll('.chip').forEach(c=>c.classList.toggle('on', c.dataset.f===filter));
  const all = rows();
  const shown = all.filter(r =>
    (filter==='all' || r.status===filter) &&
    (!q || (r.name + ' ' + (r.text||r.effect||'') + ' ' + (r.brief||'') + ' ' + (r.artist||'')).toLowerCase().includes(q)));
  const fin = all.filter(r=>r.status==='Done').length;
  document.getElementById('progtext').textContent = fin + ' / ' + all.length + ' done';
  document.getElementById('progbar').style.width = (all.length ? 100*fin/all.length : 0) + '%';

  const isCards = tab === 'cards';
  const cat = isCards ? null : CH.categories.find(c=>c.id===tab);
  let h = '';
  if (cat && cat.dims) h += '<div class="catnote">Required dimensions: ' + esc(cat.dims) + '</div>';
  h += '<table><thead><tr><th></th><th>Status</th><th>Artist</th><th>Art Brief</th><th>Name</th>';
  h += isCards ? '<th>Type</th><th>Rarity</th><th>Cost</th><th>Card Text</th><th>Upgrade</th><th>Mechanics</th>'
               : '<th>Details</th>' + (cat.assets.some(a=>a.rarity)?'<th>Rarity</th>':'') + (cat.assets.some(a=>a.orientation)?'<th>Orientation</th>':'');
  h += '<th>Required Dims</th><th>Filename</th></tr></thead><tbody>';
  for (const r of shown) {
    h += '<tr>';
    h += '<td class="thumb">' + (r.hasArt
      ? '<img loading="lazy" src="../' + esc(r.path) + '" alt="" title="click to enlarge">'
      : '<div class="none">no art</div>') + '</td>';
    h += '<td><span class="badge ' + scls(r.status) + '">' + esc(r.status) + '</span></td>';
    h += '<td class="artist">' + esc(r.artist || '—') + '</td>';
    h += '<td class="brief">' + esc(r.brief) + '</td>';
    h += '<td class="name">' + esc(r.name) + '</td>';
    if (isCards) {
      h += '<td class="meta">' + esc(r.type) + '</td><td class="meta">' + esc(r.rarity) + '</td><td class="meta">' + esc(r.cost) + '</td>';
      h += '<td class="text">' + esc(r.text) + '</td><td class="meta">' + esc(r.upgrade) + '</td><td class="meta">' + esc(r.mechanics) + '</td>';
    } else {
      h += '<td class="text">' + esc(r.effect) + '</td>';
      if (cat.assets.some(a=>a.rarity)) h += '<td class="meta">' + esc(r.rarity) + '</td>';
      if (cat.assets.some(a=>a.orientation)) h += '<td class="meta">' + esc(r.orientation) + '</td>';
    }
    h += '<td class="dims">' + esc(r.dims) + '</td>';
    h += '<td class="pathcell">' + esc(r.path ? r.path.split('/').pop() : '—') + '</td>';
    h += '</tr>';
  }
  h += '</tbody></table>';
  document.getElementById('main').innerHTML = h;
}
const tabsEl = document.getElementById('tabs');
const charsEl = document.getElementById('chars');
charsEl.innerHTML = DATA.characters.map(c=>'<span class="char" data-c="'+esc(c.key)+'">'+esc(c.label)+'</span>').join('');
charsEl.addEventListener('click', e => {
  const c=e.target.closest('.char');
  if(c && c.dataset.c!==CH.key){ CH=DATA.characters.find(x=>x.key===c.dataset.c); tab='cards'; buildTabs(); render(); }
});
tabsEl.addEventListener('click', e => { const t=e.target.closest('.tab'); if(t){tab=t.dataset.t; render();} });
document.querySelector('.controls').addEventListener('click', e => { const c=e.target.closest('.chip'); if(c){filter=c.dataset.f; render();} });
document.getElementById('q').addEventListener('input', e => { q=e.target.value.toLowerCase(); render(); });
const lb = document.getElementById('lightbox');
document.getElementById('main').addEventListener('click', e => {
  const img = e.target.closest('.thumb img');
  if (img) { lb.querySelector('img').src = img.src; lb.style.display='flex'; }
});
lb.addEventListener('click', () => lb.style.display='none');
buildTabs();
render();
</script>
</body>
</html>
`;

const outPath = path.join(root, 'pages', 'art-tracker.html');
fs.writeFileSync(outPath, html);
for (const sec of sections) {
  const other = sec._assetCats.reduce((n, c) => n + c.assets.length, 0);
  console.log(`art-tracker.html [${sec.label}]: ${sec.cards.length} cards + ${other} other assets = ${sec.cards.length + other} rows`);
  const missing = [...sec.cards.filter(r => !r.hasArt).map(r => 'card: ' + r.name),
    ...sec._assetCats.flatMap(c => c.assets.filter(a => a.path && !a.hasArt).map(a => c.title + ': ' + a.name))];
  if (missing.length) console.log(`MISSING IMAGE FILES [${sec.label}]:\n  ` + missing.join('\n  '));
}
