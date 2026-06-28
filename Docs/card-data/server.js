// Tiny zero-dependency server for the Wicken card design page.
// Serves card-designs.html + cards.json, and persists TESTED toggles back to cards.json.
//   node server.js   ->  http://localhost:7820
const http = require("http");
const fs = require("fs");
const path = require("path");

const PORT = 7820;
const DOCS_DIR = path.join(__dirname, ".."); // Docs/
const JSON_PATH = path.join(__dirname, "cards.json");
const HTML_PATH = path.join(DOCS_DIR, "card-designs.html");

function send(res, code, body, type) {
  res.writeHead(code, { "Content-Type": type, "Cache-Control": "no-store" });
  res.end(body);
}

function readData() {
  return JSON.parse(fs.readFileSync(JSON_PATH, "utf8"));
}

function writeData(data) {
  fs.writeFileSync(JSON_PATH, JSON.stringify(data, null, 2) + "\n");
}

const server = http.createServer((req, res) => {
  // POST /api/tested  { entry: "FERTILIZE", tested: true }
  if (req.method === "POST" && req.url === "/api/tested") {
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const { entry, tested } = JSON.parse(raw);
        const data = readData();
        const card = data.cards.find((c) => c.entry === entry);
        if (!card) return send(res, 404, JSON.stringify({ error: "no such entry" }), "application/json");
        card.tested = !!tested;
        writeData(data);
        send(res, 200, JSON.stringify({ ok: true, entry, tested: card.tested }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }

  if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
    return send(res, 200, fs.readFileSync(HTML_PATH), "text/html; charset=utf-8");
  }
  if (req.method === "GET" && req.url.startsWith("/cards.json")) {
    return send(res, 200, fs.readFileSync(JSON_PATH), "application/json; charset=utf-8");
  }
  send(res, 404, "Not found", "text/plain");
});

server.listen(PORT, () => {
  console.log(`Wicken card designs -> http://localhost:${PORT}`);
  console.log("Edits to TESTED are saved to Docs/card-data/cards.json. Ctrl+C to stop.");
});
