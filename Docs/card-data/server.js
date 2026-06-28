// Tiny zero-dependency server for the Wicken card design page.
// Serves card-designs.html + cards.json (+ live art state + thumbnails), and persists
// TESTED / Art-Final toggles back to cards.json.
//   node server.js   ->  http://localhost:7820
const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = 7820;
const HERE = __dirname; // Docs/card-data
const ROOT = path.resolve(HERE, "..", ".."); // repo root
const DOCS_DIR = path.join(HERE, ".."); // Docs/
const JSON_PATH = path.join(HERE, "cards.json");
const HTML_PATH = path.join(DOCS_DIR, "card-designs.html");
const BIG_DIR = path.join(ROOT, "TheWicken", "images", "card_portraits", "big");
const PLACEHOLDER = path.join(BIG_DIR, "card.png"); // the "no art" duplicate source

function send(res, code, body, type) {
  res.writeHead(code, { "Content-Type": type, "Cache-Control": "no-store" });
  res.end(body);
}
const readData = () => JSON.parse(fs.readFileSync(JSON_PATH, "utf8"));
const writeData = (d) => fs.writeFileSync(JSON_PATH, JSON.stringify(d, null, 2) + "\n");

const md5 = (buf) => crypto.createHash("md5").update(buf).digest("hex");
const bigPathFor = (entry) => path.join(BIG_DIR, entry.toLowerCase() + ".png");

let placeholderHash = null;
function getPlaceholderHash() {
  if (placeholderHash) return placeholderHash;
  try { placeholderHash = md5(fs.readFileSync(PLACEHOLDER)); } catch { placeholderHash = "_none_"; }
  return placeholderHash;
}

// Live art state for a card's BIG portrait:
//   'none'        -> file missing OR byte-identical to card.png (placeholder duplicate)
//   'placeholder' -> a real, distinct image, not yet flagged final
//   'final'       -> distinct image AND the card is flagged artFinal
function artState(card) {
  const p = bigPathFor(card.entry);
  let exists = false, dup = true;
  try {
    const buf = fs.readFileSync(p);
    exists = true;
    dup = md5(buf) === getPlaceholderHash();
  } catch { exists = false; }
  if (!exists || dup) return "none";
  return card.artFinal ? "final" : "placeholder";
}

const server = http.createServer((req, res) => {
  // --- toggles ---------------------------------------------------------
  if (req.method === "POST" && (req.url === "/api/tested" || req.url === "/api/artfinal")) {
    const field = req.url === "/api/tested" ? "tested" : "artFinal";
    const valKey = field;
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const body = JSON.parse(raw);
        const data = readData();
        const card = data.cards.find((c) => c.entry === body.entry);
        if (!card) return send(res, 404, JSON.stringify({ error: "no such entry" }), "application/json");
        card[field] = !!body[valKey];
        writeData(data);
        send(res, 200, JSON.stringify({ ok: true, entry: card.entry, [field]: card[field], art: artState(card) }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }

  // --- thumbnails: /art/<ENTRY>.png  (falls back to placeholder) --------
  if (req.method === "GET" && req.url.startsWith("/art/")) {
    const entry = decodeURIComponent(req.url.slice("/art/".length).replace(/\.png$/i, ""));
    let file = bigPathFor(entry);
    if (!fs.existsSync(file)) file = PLACEHOLDER;
    try {
      res.writeHead(200, { "Content-Type": "image/png", "Cache-Control": "no-store" });
      res.end(fs.readFileSync(file));
    } catch { send(res, 404, "no image", "text/plain"); }
    return;
  }

  // --- data with live art state ----------------------------------------
  if (req.method === "GET" && req.url.startsWith("/cards.json")) {
    const data = readData();
    getPlaceholderHash.lastReset = placeholderHash = null; // recompute each load (art may change on disk)
    data.cards.forEach((c) => {
      if (typeof c.artFinal === "undefined") c.artFinal = false;
      c.art = artState(c);
    });
    return send(res, 200, JSON.stringify(data), "application/json; charset=utf-8");
  }

  // --- page ------------------------------------------------------------
  if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
    return send(res, 200, fs.readFileSync(HTML_PATH), "text/html; charset=utf-8");
  }
  send(res, 404, "Not found", "text/plain");
});

server.listen(PORT, () => {
  console.log(`Wicken card designs -> http://localhost:${PORT}`);
  console.log("TESTED + Art-Final flags are saved to Docs/card-data/cards.json. Ctrl+C to stop.");
});
