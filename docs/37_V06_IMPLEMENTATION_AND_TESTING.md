# 37 — v0.6 Implementation and Automated Testing

> **状态更新（2026-09-29）**：用户已选择 B 为唯一开发主线，A 已归档。以下 A/B 实验流程和判定阈值保留作历史记录，不再是后续 B 开发的前置门槛；不代表实验已经通过。B 的设计约束仍适用，当前启动、验收与文档优先级见 [42 — B 开发基线](42_B_DEVELOPMENT_BASELINE.md)。


## Phase 0 — Audit / freeze baseline

Before production changes:

1. read latest `PROJECT_STATE.md`;
2. build current repository;
3. run current core / engine / multiplayer smoke suite;
4. record exact current baseline behavior for Variant A;
5. preserve/backup source as the project’s existing process requires.

## Phase 1 — Variant switch

Add explicit deterministic selection of A/B.

Requirements:

- both variants use same core multiplayer/hair/tool code;
- no duplicate full game loop;
- variant selection is logged in artifacts/report output;
- smoke scripts can launch each variant intentionally.

## Phase 2 — Continuous helicopter timeline

Implement B timeline from doc 33.

Automated assertions should cover:

- same helicopter entity persists from staging through touchdown;
- flyby occurs during build;
- downwash applies real authoritative effects;
- player input/tool effects continue during approach/descent;
- customer attention can react to helicopter proximity;
- success/failure resolves without switching control off prematurely.

## Phase 3 — Perception event model

Add/extend host-authoritative perception facts:

- visible danger event;
- sudden sound event with source position/category;
- helicopter attraction;
- notice/commit/reaction stages;
- habituation/cooldown state.

Network snapshots/RPC behavior must remain bounded and consistent with current architecture. Do not network cosmetic `?`/`!` as independent high-frequency objects if the stage can be derived from authoritative reaction facts.

## Phase 4 — Mirror occlusion

Implement deterministic mirror proxy visibility.

Tests:

- unblocked visible event triggers notice;
- player body blocks;
- magazine prop blocks;
- moving blocker away restores visibility;
- unrelated object outside path does not block;
- client/host agree on authoritative reaction state.

## Phase 5 — Bracing

Tests:

- any active player can begin brace from valid position;
- bracing substantially reduces reaction movement;
- bracing player cannot simultaneously activate primary tool;
- release restores normal behavior;
- disconnect / phase transition safely clears brace;
- no stuck authoritative state after customer reset.

## Phase 6 — B target / growth budget

- ad-style poster replaces technical target emphasis in B;
- disable/hide live shape score and technical ghost/reference from ordinary B player flow;
- retain hidden shape telemetry if cheap;
- implement finite growth reservoir for B;
- tune via repeatable solo fixture so B is fair/winnable but near-total reset is not free.

Do not tune based solely on one hand-played success.

## Phase 7 — Instrumentation

Produce machine-readable logs with timestamps for:

- variant;
- phase / helicopter timeline;
- perception events and reaction stages;
- brace start/stop/player;
- mirror blocked/unblocked perception attempts;
- sound-attention source/category;
- dangerous-tool activation;
- objective/validation result;
- active player positions/inputs sufficient for later video alignment;
- existing incident/replay IDs when available.

Add simple tooling to export an observer-friendly timeline if practical.

## Phase 8 — Regression

Run all current relevant tests plus:

- 1P B smoke;
- 2P B smoke;
- 4P B smoke;
- host loss / client disconnect as currently supported;
- full 75-second B timeline at least once;
- rendered/capture review of:
  - flyby while still working;
  - `? -> !` reaction;
  - blocked mirror;
  - brace during descent;
  - helicopter touching down while player control remains active.

Do not label subjective fun as automated acceptance.

## Deliverable state

At end, update `PROJECT_STATE.md` with:

- exactly what A and B are;
- commands to launch both;
- tests actually run and results;
- known approximations;
- the human A/B protocol still pending or completed;
- no claim that B is the final product until the pre-registered test is evaluated.
