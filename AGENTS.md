# AGENTS.md — Project Hairball B mainline

## NEXT WORK — party expansion, owner decision 2026-09-30 (supersedes conflicting points below)
- Phases 0–6 of `docs/45` are implemented (see `PROJECT_STATE.md`). Next, implement in this order:
  - **phase V**: in-game voice, `docs/47_B_IN_GAME_VOICE.md`, which has not been done yet;
  - then **phases 8→13** of `docs/49_B_PARTY_EXPANSION_REQUIREMENTS.md` (requirements) and `docs/50_B_PARTY_EXPANSION_PLAN.md` (plan, entry points, checkpoints D/E/F).
- The owner moved every non-retracted item in `docs/46_B_CANDIDATE_MECHANICS.md` into 49/50. That includes the human customer (C9), target cards, measurable style attributes, the misunderstanding generator, twist cards, co-op props (barbers ≥2), throw and catch, downed and slap revive, chair rotation, tactile feedback, group photo, shop cat, drag-when-glued, alertness, and returning-customer sessions. Use 46 only for rationale.
- Phase 7 (`docs/46_B_CONTENT_REQUIREMENTS_DRAFT.md`) still needs owner confirmation.
- Constraints G-1…G-12 (44 and 49) apply. Private information (target cards, twist secrets) must never enter broadcast snapshots before the reveal. Rules must be system-enforced, not honor-based. Comfort-risky view changes must be toggleable and default off until tested internally.

## Party-loop direction — owner decision, 2026-09-30 (phases 0–6, implemented)
- The next B work is the party core loop in `docs/43_B_PARTY_LOOP_DESIGN.md` (intent), `docs/44_B_PARTY_LOOP_REQUIREMENTS.md` (requirements R1–R7) and `docs/45_B_PARTY_LOOP_IMPLEMENTATION_PLAN.md` (phase order, code entry points, tests). Implement phases 1→6 in order. Phase 7 (new commissions, customer traits, random boss) needs its requirements refined and owner-confirmed first.
- Still one shared head for 1–4 players. Never add per-player customers.
- Phase 2 replaces two things. The commission object is now carried from a table and placed by hand, instead of the call button and flight schedule. The final validation is now the customer walking out, instead of the approaching helicopter. Keep attention, brace, replay and network infrastructure.
- These are authorized only within their phases in doc 45: table commission props, molds, per-bottle growth, wig stands, secret per-player comic tasks (no score), photo gallery and a factual action log (not an awards economy). Progression and spending, ranked awards, fixed roles and precision brushes remain excluded.
- **Microphone exclusion lifted (owner, 2026-09-30).** Built-in in-game voice replaces external voice for all players. Its species voice-filter pipeline is specified in `docs/47_B_IN_GAME_VOICE.md`. Implement it as phase V, now scheduled first in the next work (see top). Never store audio.
- Player count stays 1–4.
- `docs/48_B_GAMEPLAY_OVERVIEW.md` is a one-page human overview. `docs/46_B_CANDIDATE_MECHANICS.md` is design rationale only; the executable versions are in 49/50.
- Attribution must use explicit logged actions. Never use `Patch.Source`/`BurnSource`, which are last-writer fields, to name culprits.

## Current product direction — owner decision, 2026-09-29
- **B is the sole active development baseline. A is archived historical reference.** New features, fixes, presentation, packaging and human playtests build on B.
- Read `PROJECT_STATE.md` and `docs/42_B_DEVELOPMENT_BASELINE.md` first. This decision supersedes older requirements to keep conducting an A/B experiment, await an A/B winner, or develop both variants equally. Do not restart prior migration plans.
- Retain shared infrastructure and the recoverable A snapshot/index at `docs/archive/variant-a/README.md`. Do not delete shared target/scoring code merely because A is archived. A-specific launches/regressions are opt-in; keep shared logic/engine regression coverage.
- B remains function-first: living customer attention, approaching physical validation, live work, shared success and readable failure reasons. Existing scope exclusions remain; selecting B does not authorize new customers, commissions, progression, awards or precision brushes (see the 2026-09-30 sections above for lifted items, including microphone/voice).
- This is the owner's direction choice, not proof that an A/B study passed. Preserve historical experiment evidence and thresholds. Canada–China network/comfort acceptance remains open.
- Use `scripts/run_b.ps1` and `scripts/verify_b.ps1`; normal solo/host/client/local-4P scripts select B. Historical low-level QA/lab fixtures can retain their legacy mode explicitly as documented.


## Network maintenance evidence (2026-09-29)
- LAN/final-state smoke is not evidence of WAN motion smoothness. Network changes require the seeded UDP impairment runner and motion metrics as well as gameplay facts; real route / hardware / comfort acceptance stays separate.
- Replaying CharacterBody3D input requires restoring its ground contact cache, not only position and velocity. Keep jump, wall and physical stair replay checks in AccessChecks.
- Discover existing source paths with `rg --files` before reading them; do not repeatedly guess filenames from class names.
- Path lookup and reads are dependent operations: inspect the returned `rg --files` paths first, then copy the exact path into the next read. Do not queue a guessed read in the same call as discovery (repeated during the party-loop implementation).
- If patch context fails, read the complete current lines before retrying. Do not reconstruct context from truncated output or reordered search fragments.
- Concurrent network harness runs need distinct UDP port pairs and process-unique log names. Finish a build before starting its regression batch; do not mix a replaced assembly with already-running peers. In PowerShell, use `rg -g '*.cs' directory` rather than passing wildcard file paths literally.
- The seeded runner's output directory must include milliseconds, port and PID. Seconds-only names collided in a parallel phase-V baseline/voice comparison; those interrupted runs are invalid evidence. Preserve this uniqueness in future harness edits.
- Freeze an exported candidate before concurrent long-running network checks. Nested verification scripts may rebuild; inspect their entrypoints first, and stop the owned verification process tree before replacing source assemblies. Do not label a suite spanning different builds a unified pass.
- Measure network fault windows and endurance duration with a monotonic wall clock, separately from accumulated physics time; heavy multi-process test load can drop simulation ticks. Interrupted runs are not completed endurance evidence.

You are the primary implementation owner of this repository. The human's role should be mostly playtesting and product acceptance. Your job is to inspect the current B build, implement the requested changes incrementally, compile and test when affected, fix regressions, and keep moving without routine approval gates.

## 1. Autonomous operating mode

Do not repeatedly ask the human ordinary implementation questions.

When information is missing:
- choose the simplest reversible option consistent with the specs,
- prefer testable code-first solutions,
- record meaningful decisions in `PROJECT_STATE.md`,
- continue.

Only ask when truly blocked by credentials/secrets, an unavailable proprietary dependency with no substitute, or a genuinely irreversible high-impact product decision not resolved by the docs.

Do not stop after planning, scaffolding, one milestone, a compile error, or an ordinary technical decision.

## 2. Migration rule: preserve systems, replace obsolete assumptions

This pack changes the product direction from **one NPC customer per player** to **1–4 players sharing one customer/head**.

Before editing:
1. inspect the repository,
2. identify working systems,
3. create/update `PROJECT_STATE.md`,
4. note which v0.2 systems can be reused,
5. migrate incrementally rather than deleting the project.

Prefer adapting:
- player controller,
- networking,
- HairChunk/HeadMaterial,
- tool framework,
- tool effects,
- VFX,
- event/incident tracking,
- replay infrastructure.

Remove/refactor product assumptions such as:
- player-owned customer stations,
- four independent goals,
- individual primary score competition,
- early individual submit/cash-out,
- submitted-station protection.

Do not keep dead product logic just because it already exists. Delete obsolete code once its replacement is verified and references/tests are updated.

## 3. Locked game experience

Main gameplay is **first-person 3D**.

The core mode is:
- 1–4 players,
- one central living customer,
- one shared head/material state,
- one shared absurd engineering goal,
- free tool choice,
- players physically crowd around the same subject,
- customer micro-reactions can spoil precision,
- tools can affect customer, players, player hair, props, and environment,
- the team shares success/failure and money,
- individual post-round awards are comic attribution only, not the main win condition.

The intended emotional loop is:

> Everyone is trying to help -> methods conflict -> the customer/world reacts -> a small problem becomes a chain accident -> everyone scrambles to save the same head -> physical validation reveals whether the ridiculous construction actually works.

Do not turn this into fixed-role salon workflow, a class system, or a conventional deathmatch.

## 4. Controls

Keep controls FPS-simple:
- WASD move,
- mouse look,
- E context interact / pick up / place,
- G drop current tool,
- LMB primary tool action,
- RMB secondary action when needed,
- R reload/reset only when meaningful.

One main held tool. No permanent hotbar/tool wheel as primary interaction.

Complexity must come from tool consequences and physical interactions, not input complexity.

## 5. Generic interaction architecture

Prefer a compact effect vocabulary over pairwise tool/hair hard-coding:
- AddMaterial / AddHair,
- RemoveMaterial,
- ApplyForce,
- Wet,
- Heat,
- Cool,
- Ignite,
- ChangeStiffness,
- Glue,
- Anchor,
- Detach,
- Transfer,
- CutPlane,
- Puncture,
- Damage / PanicStimulus.

Flow:

`tool input -> authoritative query/hit -> generic effects -> HeadMaterial/HairSystem + world targets -> authoritative facts -> presentation`

A tool should generally affect the world according to its rule, not only hair. A blower pushes loose props and players; fire can spread; suction can tug players through their hair; water can wet surfaces.

## 6. Head material abstraction

Keep the prototype hair implementation, but do not hard-lock architecture to human hair forever.

Use a concept such as `IHeadMaterial`, `HeadMaterialSystem`, or equivalent so later customers can use:
- hair,
- wool,
- feathers,
- tentacles,
- plant growth,
- other chunky deformable head material.

Do not over-generalize prematurely. Human hair remains the baseline; expose replaceable behavior at clean seams.

## 7. Customer behavior principle

The customer is a **living, semi-cooperative workpiece**, not a static prop and not an enemy.

Customer motion should be:
- small,
- legible,
- causally triggered,
- preceded by an anticipation cue where practical,
- capable of causing accidents,
- rare enough not to become constant annoyance.

Follow `docs/17_CUSTOMER_BEHAVIOR_AND_PANIC_SPEC.md`.

## 8. Multiplayer authority

Host owns authoritative gameplay facts:
- customer/head state,
- tool hits/effects,
- player body-impact results,
- panic state and reaction triggers,
- goal/validation state,
- shop money/result,
- incidents,
- round state.

Clients request actions. Do not network every cosmetic hair transform. Synchronize compact gameplay state and reproduce cosmetic wobble/debris locally where feasible.

## 9. Code-first Godot

Target Godot 4.7.2 stable Mono/.NET + C#.

Avoid routine manual editor work for the human. Repeated setup should be generated/configured in code or tooling. Use primitives/generated low-poly meshes and simple materials when art is missing.

If editor paths are unknown, support `GODOT_BIN` and search reasonable locations before blocking.

## 10. Validation and testing discipline

After meaningful changes:
1. compile,
2. run unit/logic tests,
3. run available Godot smoke tests,
4. run network smoke tests when affected,
5. fix regressions,
6. update `PROJECT_STATE.md`,
7. continue.

Do not claim something is verified unless it was actually run/observed.

## 11. Prototype art and reference images

Read `docs/16_VISUAL_REFERENCE_GUIDE.md` and inspect `docs/reference/`.

The images are intent references, not assets to copy. Prioritize:
1. readability of cause/effect,
2. huge readable shared customer/head,
3. four bodies physically crowding the subject,
4. chunky low-poly deformable material,
5. absurd tools and validation props,
6. comedy through physical consequence.

Do not waste prototype time recreating decorative UI/text from the images.

## 12. Product guardrails

Do not add unless required by acceptance:
- realistic shampoo/cut/dry mandatory workflow,
- fixed player jobs/classes,
- deep shop management,
- campaign,
- ranked competitive scoring,
- complex customer combat AI,
- gore,
- realistic hair strands,
- dedicated servers,
- matchmaking,
- paid middleware.

## 13. Completion behavior

Continue through the current authorized B task without routine approval between milestones. `docs/11_IMPLEMENTATION_PLAN.md` is historical migration context, not an instruction to redo completed work.

At the end of the run, report only:
- what is actually playable,
- exact build/run commands,
- tests actually run and results,
- genuine remaining acceptance gaps/blockers,
- highest-value items for the next human playtest.

## C# partial-file compilation note (2026-09-30)
- Each partial file needs its own using directives. GetValueOrDefault on Dictionary requires System.Collections.Generic even when another partial file imports it; include that namespace before compiling new bootstrap partials. Re-check local imports whenever adding Vector3/Quaternion or another namespace-dependent type to an existing partial as well; do not rely on other partial files. Two missing-import compile failures were corrected without changing runtime behavior.

## Audio shutdown evidence (2026-09-30)
- Root `_ExitTree` runs after child exit notifications. Leave child audio stop/destruction to their engine notifications there; stop/clear them explicitly only during an ordinary live room transition. Native 0xc0000005 exits recurred at export offset 0x249aa4b in c2/c5; a single non-recurrence run did not prove a shutdown repair. Require repeated frozen 4P exit checks and retain failed candidates.
- Generator input is always resampled/decoded 48 kHz: explicitly select Custom mix-rate mode. The network jitter queue and the generator ring are separate latency sources; do not stack two large buffers. Record actual playout age and ring occupancy, never zero an estimate because the transport closed.

## Source-path discipline (2026-09-30)
- Before reading an unfamiliar file, run rg --files on the relevant directory, inspect the returned paths, then read the exact existing path in the next call. Do not combine discovery with a guessed read. Use rg -g '*.cs' src/Core for file patterns; Windows rg does not expand literal wildcard paths. Repeated guessed script paths were caught and corrected.
- Engine fixtures cannot reference helpers compiled only into tests/Core (e.g. GoalFixtures); use an existing engine fixture or its geometry directly. Re-check imports after adding LINQ queries to a presentation file; a second missing-import failure in ContactFeedback exposed this again.


## Ephemeral-state QA discipline (2026-09-30)
- For network accident fixtures, wait from the explicit effect application's logged time on the intended victim, not from QA stage entry or a different actor's hit. A tool switch can precede the first correct input under latency. Keep a bounded observation window before rescue; production rescue remains immediate. Repeated one-frame fixture failures were retained, then fixed this way.
- When a network fixture injects actor positions, use the same pose rule on the host and client before computing aim in every stage, including fallen-object pickup. Fixing only the raised placement stage leaves stale snapshot origins able to generate unrelated interactions. C2 four-player failures exposed this second instance; keep the failures as evidence.
- Voice startup latency must recover from a delayed first packet: fixed sequence playback deadlines can preserve permanent backlog even when arrival latency is normal. Measure packet arrival age, actual generator queue and playout age separately; retain a stale-frame catch-up regression.
- A fixed engine buffer threshold can become vacuous after resizing the ring. Compare GetFramesAvailable before and after PushBuffer to verify the actual path.
- Native audio shutdown failures recurred after single successful exits. Keep explicit AudioStreamGenerator ownership, stop and drain in the live tree before releasing playback/stream resources; route window-close and QA quits through that same path. Verify repeated exits; upstream similarity alone is not local root-cause proof.
- Small voice generator rings cannot validate real-time playback through Godot's large-block Dummy mixer. In headless voice QA, keep the actual generator/codec path but use muted WASAPI (`-RealAudio`) for mouth-to-ear estimates on this Windows host; record both paths, discarded samples and mixer time. Dummy-only decoder counts and a latency average are insufficient playback evidence.
- `LandingFeedback.cs` is in `src/Core`, despite appearing next to presentation entry points in the plan. A partial filename in a document is not an existing source path; discover the exact path before reading.

## API and path verification (2026-09-30)
- Read the discovered API declaration before adding a new call: Hud.Update requires authority, lab, portrait and byte count. Godot Vector3 properties such as Node3D.Rotation return copies; assign the complete vector, not Rotation.Y/Z. Keep file discovery separate from reading, including after compaction; Core motion types live in MotionNet.cs and HUD partials under src/UI.
