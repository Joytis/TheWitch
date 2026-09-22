/*
 * characters.js — the ONE place the card-doc tooling learns about each character mod in this repo.
 *
 * Imported by regen.js, server.js and ../art-tracker/regen-art-tracker.js. All paths are repo-root
 * relative (join them with the caller's ROOT). card-designs.html carries a browser-side mirror of the
 * label / data file / prefix / mechanics fields in its CLASSES table — keep the two in sync.
 *
 * Adding a character = add an entry here + its briefs/assets JSON files (empty shapes are fine).
 */
const CHARACTERS = {
  witch: {
    key: "witch",
    id: "TheWitch",
    label: "The Witch",
    shortLabel: "Witch",
    cardsDir: "TheWitch/TheWitchCode/Cards",                       // recursed (+ Familiar/ subdir)
    loc: "TheWitch/TheWitch/localization/eng/cards.json",
    powersLoc: "TheWitch/TheWitch/localization/eng/powers.json",
    prefix: "THEWITCH-",
    skipClasses: ["WitchCard", "WitchFamiliarCard", "IFamiliarSummon", "FamiliarCardRegistry"],
    spawnedPool: "WitchSpawnedCardPool",                   // cards bound here doc as rarity "Special"
    portraits: "TheWitch/TheWitch/images/card_portraits",
    portraitSubdirs: ["familiar"],                         // extra source-art subdirs to probe
    dataFile: "Docs/card-data/cards.json",                 // keep this filename — downstream tooling reads it
    briefs: "Docs/art-tracker/card-briefs.json",
    assets: "Docs/art-tracker/assets.json",
    mechanics: ["Brambles", "Potions", "Familiars", "Hex", "None"],
  },
  augur: {
    key: "augur",
    id: "TheAugur",
    label: "The Augur",
    shortLabel: "Augur",
    cardsDir: "TheAugur/TheAugurCode/Cards",
    loc: "TheAugur/TheAugur/localization/eng/cards.json",
    powersLoc: "TheAugur/TheAugur/localization/eng/powers.json",
    prefix: "THEAUGUR-",
    skipClasses: ["AugurCard"],
    spawnedPool: null,
    portraits: "TheAugur/TheAugur/images/card_portraits",
    portraitSubdirs: [],
    dataFile: "Docs/card-data/augur.json",
    briefs: "Docs/art-tracker/augur-briefs.json",
    assets: "Docs/art-tracker/augur-assets.json",
    mechanics: ["Foretell", "Omen", "Augury", "None"],
  },
};

const KEYS = Object.keys(CHARACTERS);

// Resolve a `--character` value ("witch" | "augur" | "all" | undefined) to a list of configs.
function select(key) {
  if (!key || key === "all") return KEYS.map((k) => CHARACTERS[k]);
  const ch = CHARACTERS[String(key).toLowerCase()];
  if (!ch) throw new Error(`unknown character "${key}" (expected one of: ${KEYS.join(", ")}, all)`);
  return [ch];
}

// Find the character whose data file has this basename (e.g. "cards.json" -> witch).
function byDataFile(basename) {
  return KEYS.map((k) => CHARACTERS[k]).find((ch) => ch.dataFile.split("/").pop() === basename) || null;
}

module.exports = { CHARACTERS, KEYS, select, byDataFile };
