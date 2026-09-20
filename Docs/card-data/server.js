// Tiny zero-dependency server for the card design page (every character in characters.js).
// Serves card-designs.html + each character's data file (+ live art state + thumbnails), and persists
// TESTED / Art-Final toggles back to that file. Every write endpoint takes a `character` key
// (default "witch" for back-compat); GET routes take it as ?character= / ?c=.
//   node server.js   ->  http://localhost:7820
const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = 7820;
const HERE = __dirname; // Docs/card-data
const ROOT = path.resolve(HERE, "..", ".."); // repo root
const DOCS_DIR = path.join(HERE, ".."); // Docs/
const HTML_PATH = path.join(DOCS_DIR, "card-designs.html");
const characters = require("./characters");

// Resolve a character key ("witch" default) to its config + absolute paths.
function charOf(key) {
  const ch = characters.CHARACTERS[String(key || "witch").toLowerCase()];
  if (!ch) throw new Error(`unknown character "${key}"`);
  return {
    ...ch,
    jsonPath: path.join(ROOT, ch.dataFile),
    briefsPath: path.join(ROOT, ch.briefs),
    bigDir: path.join(ROOT, ch.portraits), // single-size source art (packed to atlases at publish)
    placeholder: path.join(ROOT, ch.portraits, "card.png"), // the "no art" duplicate source
  };
}
const query = (url) => Object.fromEntries(new URL(url, "http://x").searchParams);
const charFromUrl = (url) => { const q = query(url); return charOf(q.character || q.c); };

function send(res, code, body, type) {
  res.writeHead(code, { "Content-Type": type, "Cache-Control": "no-store" });
  res.end(body);
}
const readData = (ch) => JSON.parse(fs.readFileSync(ch.jsonPath, "utf8"));
const writeData = (ch, d) => fs.writeFileSync(ch.jsonPath, JSON.stringify(d, null, 2) + "\n");

// Artist + art-brief per card (hand-authored via the page) -> Docs/art-tracker/<char>-briefs.json
const readBriefs = (ch) => {
  try { const d = JSON.parse(fs.readFileSync(ch.briefsPath, "utf8")); d.briefs ||= {}; return d; }
  catch { return { briefs: {} }; }
};
const writeBriefs = (ch, d) => fs.writeFileSync(ch.briefsPath, JSON.stringify(d, null, 2) + "\n");

// Regenerate the static art-tracker page (the team-facing view) after any write.
// Debounced + fire-and-forget so rapid edits don't stack processes.
let regenTimer = null;
function regenTracker() {
  clearTimeout(regenTimer);
  regenTimer = setTimeout(() => {
    const child = require("child_process").execFile(
      process.execPath, [path.join(DOCS_DIR, "art-tracker", "regen-art-tracker.js")],
      (err) => { if (err) console.error("art-tracker regen failed:", err.message); });
    child.unref?.();
  }, 400);
}

const md5 = (buf) => crypto.createHash("md5").update(buf).digest("hex");
// Source art lives at card_portraits/<entry>.png, but some cards author theirs under a subdir —
// Witch familiar token cards use familiar/<entry>.png (mirrors WitchFamiliarCard's `familiar/` atlas-key
// prefix; characters.js `portraitSubdirs`). Check the root first, then each subdir; fall back to the
// root path when none exists.
const bigPathFor = (ch, entry) => {
  const root = path.join(ch.bigDir, entry.toLowerCase() + ".png");
  if (fs.existsSync(root)) return root;
  for (const sub of ch.portraitSubdirs) {
    const p = path.join(ch.bigDir, sub, entry.toLowerCase() + ".png");
    if (fs.existsSync(p)) return p;
  }
  return root;
};
const hashFile = (file) => { try { return md5(fs.readFileSync(file)); } catch { return null; } };

// Known placeholder images (the generic card backs). Any big art matching one of these is "No Art".
const placeholderFiles = (ch) => [
  ch.placeholder, // card_portraits/card.png
];

// Live art state for every card's BIG portrait, recomputed from disk on each call:
//   'none'        -> file missing, OR equals a known card.png, OR is a duplicate shared by >1 card
//                    (a shared image is a placeholder, not finished unique art)
//   'placeholder' -> a real, distinct image (unique to this card), not yet flagged final
//   'final'       -> a real, distinct image AND the card is flagged artFinal
function computeArtStates(ch, cards) {
  const placeholders = new Set(placeholderFiles(ch).map(hashFile).filter(Boolean));
  const hashByEntry = {};
  const counts = {};
  for (const c of cards) {
    const h = hashFile(bigPathFor(ch, c.entry));
    hashByEntry[c.entry] = h;
    if (h) counts[h] = (counts[h] || 0) + 1;
  }
  const states = {};
  for (const c of cards) {
    const h = hashByEntry[c.entry];
    if (!h || placeholders.has(h) || counts[h] > 1) states[c.entry] = "none";
    else states[c.entry] = c.artFinal ? "final" : "placeholder";
  }
  return states;
}

const server = http.createServer((req, res) => {
  // --- toggles ---------------------------------------------------------
  const TOGGLE_FIELDS = { "/api/tested": "tested", "/api/artfinal": "artFinal", "/api/vfxpass": "vfxPass", "/api/sfxpass": "sfxPass" };
  if (req.method === "POST" && TOGGLE_FIELDS[req.url]) {
    const field = TOGGLE_FIELDS[req.url];
    const valKey = field;
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const body = JSON.parse(raw); // { entry, <field>, character? }
        const ch = charOf(body.character);
        const data = readData(ch);
        const card = data.cards.find((c) => c.entry === body.entry);
        if (!card) return send(res, 404, JSON.stringify({ error: "no such entry" }), "application/json");
        card[field] = !!body[valKey];
        writeData(ch, data);
        if (field === "artFinal") regenTracker(); // Done-status feeds the static tracker
        const art = computeArtStates(ch, data.cards)[card.entry];
        send(res, 200, JSON.stringify({ ok: true, entry: card.entry, [field]: card[field], art }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }

  // --- artist + art brief per card --------------------------------------
  if (req.method === "POST" && req.url === "/api/brief") {
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const body = JSON.parse(raw); // { name, artist, brief, character? }
        if (!body.name) return send(res, 400, JSON.stringify({ error: "name required" }), "application/json");
        const ch = charOf(body.character);
        const data = readBriefs(ch);
        const artist = String(body.artist || "").trim();
        const brief = String(body.brief || "").trim();
        if (!artist && !brief) delete data.briefs[body.name];
        else data.briefs[body.name] = { artist, brief };
        writeBriefs(ch, data);
        regenTracker();
        send(res, 200, JSON.stringify({ ok: true, name: body.name, artist, brief }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }
  if (req.method === "GET" && req.url.startsWith("/briefs.json")) {
    try { return send(res, 200, JSON.stringify(readBriefs(charFromUrl(req.url))), "application/json; charset=utf-8"); }
    catch { return send(res, 200, JSON.stringify({ briefs: {} }), "application/json"); }
  }

  // --- thumbnails: /art/<ENTRY>.png?c=<character>  (falls back to placeholder) --------
  if (req.method === "GET" && req.url.startsWith("/art/")) {
    const reqPath = req.url.split("?")[0]; // drop ?v= cache-buster / ?c= before parsing
    const entry = decodeURIComponent(reqPath.slice("/art/".length).replace(/\.png$/i, ""));
    let ch;
    try { ch = charFromUrl(req.url); } catch { return send(res, 404, "no such character", "text/plain"); }
    let file = bigPathFor(ch, entry);
    if (!fs.existsSync(file)) file = ch.placeholder;
    try {
      res.writeHead(200, { "Content-Type": "image/png", "Cache-Control": "no-store" });
      res.end(fs.readFileSync(file));
    } catch { send(res, 404, "no image", "text/plain"); }
    return;
  }

  // --- data with live art state: /cards.json (witch), /augur.json, ... (characters.js dataFile)
  const dataChar = req.method === "GET" && characters.byDataFile(req.url.split("?")[0].slice(1));
  if (dataChar) {
    const ch = charOf(dataChar.key);
    if (!fs.existsSync(ch.jsonPath)) return send(res, 404, `Run node Docs/card-data/regen.js --character ${ch.key}`, "text/plain");
    const data = readData(ch);
    const states = computeArtStates(ch, data.cards);
    data.cards.forEach((c) => {
      if (typeof c.artFinal === "undefined") c.artFinal = false;
      if (typeof c.vfxPass === "undefined") c.vfxPass = false;
      if (typeof c.sfxPass === "undefined") c.sfxPass = false;
      c.art = states[c.entry];
    });
    return send(res, 200, JSON.stringify(data), "application/json; charset=utf-8");
  }

  // --- base-game reference data (read-only): /silent.json, /necrobinder.json
  if (req.method === "GET" && /^\/(silent|necrobinder|ironclad|defect|regent)\.json/.test(req.url)) {
    const name = req.url.split("?")[0].slice(1);
    try {
      return send(res, 200, fs.readFileSync(path.join(HERE, name)), "application/json; charset=utf-8");
    } catch { return send(res, 404, "Not found", "text/plain"); }
  }

  // --- page ------------------------------------------------------------
  if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
    return send(res, 200, fs.readFileSync(HTML_PATH), "text/html; charset=utf-8");
  }
  send(res, 404, "Not found", "text/plain");
});

server.listen(PORT, () => {
  console.log(`Card designs (${characters.KEYS.join(", ")}) -> http://localhost:${PORT}`);
  console.log("TESTED + Art-Final + VFX/SFX-Pass flags are saved to each character's Docs/card-data/*.json. Ctrl+C to stop.");
});
