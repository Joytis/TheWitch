---
name: release
description: Cut and ship a Workshop release end to end - version bump, patch-notes review, release commit, then the deploy pipeline (verify both game branches, upload public + beta, tag, push). Use when the user says "/release", "ship a release", "deploy the mod", "cut and deploy", or wants a new version on the Steam Workshop.
---

# Release

Three steps, in order: **version bump → patch-notes review → deploy**. The deploy is
`tools/deploy.ps1`, the single source of the pipeline (stages, state, failure reports); this skill
only drives it. Default character: Witch.

Everything from step 4 on is outward-facing or writes git history. This skill is the user's standing
authorization for exactly two things, each still confirmed in the moment: the release commit in
step 3, and launching the deploy in step 4. The deploy script makes its own commit (marketing
images), tag and push. Never tag, push or upload by hand, and never add attribution trailers.

## 1. Preconditions

- `git status --short` and `git rev-parse --abbrev-ref HEAD`: must be on `main`. Any uncommitted
  change that is not release material (see step 3) stops the release: list the files and ask the
  user to commit or discard them. Do not commit unrelated work.
- Steam running, Slay the Spire 2 closed, both `game-installs/public` and `game-installs/beta`
  present. `./tools/deploy.ps1 -Plan` is a cheap sanity check; the pipeline's preflight is the
  authority and reports anything missing.

## 2. Version bump + patch notes

Run the `version-bump` skill (Skill tool) through its bump and patch-notes steps. Then review:

- Print the full notes file in the reply (the user reviews it there) with the version and the ref
  it was diffed against.
- These notes are what players read: they become the Steam change note (converted to BBCode by the
  upload) and the Discord announcement.
- Ask with AskUserQuestion: approve, or edit. Apply requested edits and show the notes again. Loop
  until approved. Never proceed on silence.

## 3. Release commit

After the notes are approved, commit the release material, and only that:

- `<Project>/<ModId>.json` (the version bump), `Docs/patch-notes/<version>.md`, and, if the bump
  step regenerated them, `Docs/card-data/*.json` + `pages/art-tracker.html`.
- `git add` those paths explicitly (never `-A`), then `git commit -m "Witch <version>"` (the
  repo's release-commit style; "Augur <version>" for the Augur).
- `git status --short` must now be empty. If it is not, stop and show what is left.

## 4. Deploy

Two breakpoints: one before verification starts, one after it passes and before anything is
committed or uploaded.

Confirm the start with AskUserQuestion ("Start the v<version> release run?"), saying it first
verifies both branches (~25 minutes) and then stops at the `review` gate before marketing or any
upload. On yes, start it in the background and wait for the completion notice; do not poll:

```
powershell -NoProfile -ExecutionPolicy Bypass -File tools/deploy.ps1 -Character Witch -Yes
```

`-Yes` is required because there is no console for the script to ask on. Never start a full run for
any other reason (testing the script = `-VerifyOnly` or `-Plan`).

Then act on the exit code. State and logs: `build/deploy/<ModId>-<version>/state.json` and
`NN-<stage>.log`.

- **2 at `review`** (the first gate, after `autoslay-<last branch>`): verification passed on both
  branches and nothing has been committed or uploaded. Report each verification stage's result from
  `state.json` (publish dll hash, the `Items: N run, N passed...` smoke-test line, AutoSlay seeds and
  time per stage); skim the testall/autoslay logs for anything the summaries hide (stray errors
  allowed through, warnings). Then ask with AskUserQuestion ("Upload v<version> to the Workshop
  now?", saying it commits marketing images, uploads public, tags, uploads beta, pushes). Yes =
  `-Resume -Ack`. No = stop; the run stays resumable until HEAD moves.
- **2 at `flag-public` / `flag-beta`**. Tell the user exactly what the
  gate asks (it is in `state.json` → that stage's `detail`: mark the just-uploaded revision as that
  game branch's version on the Workshop page, with the link). When the user says it is done, continue
  with `-Resume -Ack` (same command, `-Yes` not needed). Never ack a gate the user has not confirmed.
- **1 = a stage failed**. Read the stage's `cause` from `state.json` (and its log if the cause is
  thin), and report: stage, cause, what has and has not been uploaded so far. Then, by kind:
  - Preflight problem (Steam closed, game running, stale docs): fix it with the user, `-Resume`.
  - A flaky run or a failure the user judges unrelated (e.g. a base-game bot stall): `-Resume`
    re-runs the stage; `-Resume -Skip <stage>` only if the user says to skip it.
  - A real mod bug: fix it, the user commits (or approves the commit), then `-Restart`: a source
    change invalidates the verification, so the run starts over. If `upload-public` had already
    succeeded the version is tagged and live on public: the fix ships as a NEW version, back at
    step 2.
- **0 = done**. Report the stage summary (times, AutoSlay seeds), the tag, and that both builds
  are uploaded and pushed.

## Notes

- A dry run with no side effects: `./tools/deploy.ps1 -VerifyOnly -Marketing` (VS Code task
  "Deploy: Dry Run"). Worth offering before step 2 when the tree has risky changes; the real run
  repeats the same verification either way.
- The tag belongs to the pipeline: it is created after the first upload succeeds, and preflight
  refuses a version whose tag exists. A version that is tagged is released; bump again.
- Failures and gates are also posted to the deploy Discord webhook
  (`tools/deploy-webhook.local.txt`), so the user may already know.
