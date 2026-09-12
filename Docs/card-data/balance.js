#!/usr/bin/env node
/*
 * balance.js — builds Docs/balance-dashboard.html: an interactive Witch-vs-base-classes
 * balance dashboard (damage / block per card + per energy, by rarity / cost / class,
 * rider density, upgrade deltas, outliers, card explorer).
 *
 * Reads Docs/card-data/{cards,silent,necrobinder,ironclad,defect,regent}.json, slims each
 * card to the fields the page needs, and injects the dataset into balance-template.html.
 * All statistics are computed client-side so the page's filters stay live.
 *
 *   node Docs/card-data/balance.js
 */
const fs = require("fs");
const path = require("path");

const HERE = __dirname;
const OUT = path.join(HERE, "..", "balance-dashboard.html");
const TEMPLATE = path.join(HERE, "balance-template.html");

const CLASSES = [
  ["witch", "cards"],
  ["ironclad", "ironclad"],
  ["defect", "defect"],
  ["regent", "regent"],
  ["necrobinder", "necrobinder"],
  ["silent", "silent"],
];

const KEEP = ["name", "entry", "cost", "type", "rarity", "target", "text", "numbers", "upgrade", "multiplayer", "mechanics", "role", "sub", "threads"];

const data = {};
for (const [cls, file] of CLASSES) {
  const cards = JSON.parse(fs.readFileSync(path.join(HERE, file + ".json"), "utf8")).cards;
  data[cls] = cards.map((c) => {
    const o = {};
    for (const k of KEEP) if (k in c) o[k] = c[k];
    return o;
  });
}

const payload = JSON.stringify({ generated: new Date().toISOString().slice(0, 10), classes: CLASSES.map(([c]) => c), data });
const html = fs.readFileSync(TEMPLATE, "utf8").replace("__CARD_DATA__", () => payload.replace(/<\/script/gi, "<\\/script"));
fs.writeFileSync(OUT, html);
console.log(`Wrote ${path.relative(process.cwd(), OUT)} (${(html.length / 1024).toFixed(0)} KB, ${Object.values(data).reduce((a, b) => a + b.length, 0)} cards)`);
