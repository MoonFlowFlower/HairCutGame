# AGENTS v0.6 Addendum — Core Hypothesis Experiment

This addendum supplements the repository's existing `AGENTS.md`. When this addendum conflicts with older prototype-expansion instructions, its B design constraints still apply. The owner's B-mainline decision in `docs/42_B_DEVELOPMENT_BASELINE.md` supersedes the older requirement to run a controlled A/B experiment.

## 1. Do not restart

The current project is a working Godot 4.7.2 Mono/.NET C# prototype. Audit first. Reuse working code for:

- first-person movement and collision;
- ENet / host-authoritative multiplayer;
- shared central customer/head;
- continuous solid low-poly hair volume;
- tool ownership and generic tool effects;
- customer reaction pose plumbing;
- physical loose material/debris;
- validation objects;
- replay/event recording;
- localization and developer scripts.

Do not rebuild these systems merely to make the experiment cleaner.

## 2. Current status: B mainline, A archived (2026-09-29)

The owner selected B as the development baseline. The original controlled A/B implementation phase is historical; do not require another A/B decision before continuing B. Preserve the recoverable A snapshot and shared systems; see `docs/42_B_DEVELOPMENT_BASELINE.md` and `docs/archive/variant-a/README.md`. A is an explicit historical launch, not a parallel product.

Retained low-level switches (normal development uses `scripts/run_b.ps1`):

- `--v06-variant-a`
- `--v06-variant-b`

If the project already has a cleaner experiment-fixture mechanism, integrate there instead.

## 3. Highest priority

The v0.6 implementation priority is:

1. keep control during live helicopter approach/landing;
2. make customer perception directional, readable and usable by players;
3. make mirror occlusion and temporary head bracing real gameplay actions;
4. make the helicopter’s mid-round flyby and final approach physically affect the work;
5. provide a fair function-first B target without technical tracing pressure;
6. instrument B playtests for readability, coordination, fairness and network comfort; retain historical A/B telemetry without requiring new comparison sessions.

Do not spend this pass on new content breadth.

## 4. Avoid re-entering multiplayer ZBrush

For Variant B specifically:

- do not make three-view technical sheets the primary target;
- do not show live shape percentage as the thing players optimize;
- do not require pixel/voxel-like target matching;
- do not add more precision brush modes to compensate;
- do not introduce new fine sculpt tools unless required to keep the existing game functional.

The player must understand the **result** (“land the helicopter”), not trace a prescribed solution.

## 5. Customer is a manipulable audience, not random noise

Customer reactions must be caused by perceived events and must have direction and warning.

Do not implement “random head twitch every N seconds.”

Every material gameplay reaction needs:

- a cause;
- a readable pre-telegraph;
- a directional intention when applicable;
- a short response window;
- a resulting physical pose used by actual tool/hair queries.

## 6. No microphone integration in phase 1

Do not request microphone permissions or build voice detection in the first v0.6 implementation.

Game-world sound perception is enough for the primary experiment. A Wizard-of-Oz / manual dev trigger may be added for later microphone hypothesis testing, but must not block Variant B.

## 7. Do not hard-require four jobs/roles

The experiment is not trying to prove that four fixed jobs are necessary.

Do not create permanent roles such as “head holder”, “mirror blocker”, or “stylist”. These are transient needs that any available player can satisfy.

Solo should remain technically playable. Multiplayer should create **new coordination behaviors**, not merely faster throughput.

## 8. Autonomy

Implement continuously without repeatedly asking for routine technical approval. For reversible ambiguity, choose the smallest implementation that preserves B's gameplay intent and the integrity of historical experiment evidence, document it in `PROJECT_STATE.md`, test it, and continue.

Only stop for a genuine external blocker or an irreversible product decision not answered here.
