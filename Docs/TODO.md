# TODO

ISSUE: Mod compatibility: If we have a BUNCH of potions, Separatory Funnel Overlay can't scroll!
I love this character super cute, and I love the different builds too go into. I'm doing a modded arcipelago randomizer run where I threw in a few modded characters and she's one of my favorites. the only thing is (and this might be because I have 5 modded characters so there's too many potions) whenever I have the upgraded relic where I choose a potion when I make a random one, there's too many potions on the screen and no way too scroll through them/change grid size or anything? I was gonna try to do something with brambles and just choose the brambles potion but unfortunately it's not any of the 20 or so on my screen.

Work queue for TheWitch mod. New items come from [TODO_STAGING.md](TODO_STAGING.md); protocol lives in the `todo-loop` skill (`.claude/skills/todo-loop/SKILL.md`). Statuses: `TODO` · `BLOCKED (reason)`; done items → [DONE.md](DONE.md).

## Witch combat animation feedback (Kitsu, vua — 2026-11-16)

All in `TheWitch/TheWitch/animation/raw/witch_combat.spine`; re-export with `tools/export-spine.ps1 -Character Witch -Only witch_combat`. Praised, keep: potion bottle bounce on hit, chest spring on the hurt hop-back, summon animation overall.

### 1. Hurt: eyes close into a bracing face — TODO
**Rule:** Eyes close into a ">w<" / "> <" bracing face for most of `hurt` (currently open throughout).

### 2. Hurt: ears swivel back like cast — TODO
**Rule:** Ears swivel back with the same motion as `cast` instead of facing forward and getting knocked back; much less bounce.

### 3. Hurt: both feet lift together on the recovery hop — TODO
**Rule:** Both feet leave the ground at about the same time on the hop back up (currently rear foot first).

### 4. Idle: tail undulates through to the tip — TODO
**Rule:** `idle_loop` tail motion is a gentle wave that travels all the way to the tip, not a stiff mass moving up and down as a whole.

### 5. Cast/summon: wiggle bones on the flames — TODO
**Rule:** Add bones to the summon flames with weights and a bit of wind so they wave.

### 6. Physics tuning pass across all animations — BLOCKED (wait until the remaining animations exist)
**Rule:** Balance secondary physics (hair, dress, chest) so idle isn't stiff and hurt isn't floppy; reads closer to base-game strength. Keep the potion bounce and hop-back chest spring.
