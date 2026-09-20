#!/usr/bin/env node
/*
 * balance.js — builds Docs/balance-dashboard.html: an interactive mod-character-vs-base-classes
 * balance dashboard (damage / block per card + per energy, by rarity / cost / class,
 * rider density, upgrade deltas, outliers, card explorer).
 *
 * Reads every mod character's data file from characters.js (Witch = cards.json, Augur =
 * augur.json, ...) plus Docs/card-data/{silent,necrobinder,ironclad,defect,regent}.json, slims
 * each card to the fields the page needs, and injects the dataset into balance-template.html.
 * All statistics are computed client-side so the page's filters stay live; the page carries a
 * focus-character switcher (persisted in localStorage) that picks which mod character the
 * tiles / outliers / token chip describe.
 *
 *   node Docs/card-data/balance.js
 */
const fs = require("fs");
const path = require("path");
const characters = require("./characters");

const HERE = __dirname;
const OUT = path.join(HERE, "..", "balance-dashboard.html");
const TEMPLATE = path.join(HERE, "balance-template.html");

// Mod characters first (from characters.js), then the base classes. "Base" on the page =
// every class that is not a mod character.
const MODS = characters.KEYS.map((k) => {
  const ch = characters.CHARACTERS[k];
  return { key: ch.key, label: ch.label, shortLabel: ch.shortLabel, file: path.basename(ch.dataFile, ".json") };
});
const BASE_CLASSES = [
  ["ironclad", "ironclad"],
  ["defect", "defect"],
  ["regent", "regent"],
  ["necrobinder", "necrobinder"],
  ["silent", "silent"],
];
const CLASSES = [...MODS.map((m) => [m.key, m.file]), ...BASE_CLASSES];

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

const payload = JSON.stringify({
  generated: new Date().toISOString().slice(0, 10),
  classes: CLASSES.map(([c]) => c),
  mods: MODS.map(({ key, label, shortLabel }) => ({ key, label, shortLabel })),
  data,
});
const html = fs.readFileSync(TEMPLATE, "utf8").replace("__CARD_DATA__", () => payload.replace(/<\/script/gi, "<\\/script"));
fs.writeFileSync(OUT, html);
console.log(`Wrote ${path.relative(process.cwd(), OUT)} (${(html.length / 1024).toFixed(0)} KB, ${Object.values(data).reduce((a, b) => a + b.length, 0)} cards, mods: ${MODS.map((m) => m.key).join(", ")})`);
