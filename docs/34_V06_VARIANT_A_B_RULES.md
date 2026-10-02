# 34 — Variant A / B Rules

> **状态更新（2026-09-29）**：用户已选择 B 为唯一开发主线，A 已归档。以下 A/B 实验流程和判定阈值保留作历史记录，不再是后续 B 开发的前置门槛；不代表实验已经通过。B 的设计约束仍适用，当前启动、验收与文档优先级见 [42 — B 开发基线](42_B_DEVELOPMENT_BASELINE.md)。


The test must keep both variants launchable from the same codebase.

## Variant A — Current baseline

Purpose: represent the current product direction as faithfully as practical.

Use the repository’s current shared-head helipad flow and target/scoring/reference behavior. Do not intentionally sabotage A to make B look better.

If current code changes before this pack is applied, snapshot/document exactly what “A” means in `PROJECT_STATE.md` before editing B.

## Variant B — Function-first living-customer experiment

### Mission language

Hard player-facing requirement:

> **Land the helicopter on the customer’s hair and keep it stable.**

Optional lightweight wording may clarify that the customer should remain manageable/safe, but do not prescribe the construction method.

### Target presentation

Use one **ad-style customer fantasy image/poster** showing the desired outcome.

The image should emphasize:

- stylish/humorous successful helicopter landing;
- result, not construction geometry.

Do not show technical orthographic three-views as the primary B reference.

Do not use an inspectable exact miniature target as a construction blueprint in the first B experiment.

Do not show a live shape-match percentage in B.

The existing exact target/shape scorer may remain active as **hidden telemetry** to help later analysis, but it must not drive player feedback or B success.

### B success

Primary success is physical/function-based:

1. helicopter can contact/land on the produced hair support;
2. remains acceptably supported/stable for a configured dwell (target ~3 seconds);
3. catastrophic customer/commission failure conditions are not triggered.

Use the existing validation proxy/physics architecture rather than requiring high-fidelity rigidbody/aerodynamic simulation.

### Growth supply

Growth is finite in B.

Internal calibration requirement before external A/B test:

- a competent solo developer must be able to complete a viable helipad using normal safe play;
- the available growth should support the intended build with modest correction;
- it should **not** support shaving most/all of the head and freely rebuilding from scratch.

Do not tune the can so tightly that B is mostly unwinnable.

### Dangerous tools

Do not add new dangerous tools merely for B.

Use current tools. The experiment asks whether pressure makes players choose high-risk tools for a **job reason** rather than for novelty/trolling.

### No new content breadth

B test uses the helipad mission only.

The bird-nest image in `reference_v06` is a future physical-prop reminder, not part of the primary A/B implementation.
