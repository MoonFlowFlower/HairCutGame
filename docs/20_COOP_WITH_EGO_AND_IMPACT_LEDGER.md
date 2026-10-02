# 20 — Co-op With Ego + Gameplay Impact Ledger

## 1. Design objective

v0.3 proved the technical shared-head structure. v0.4 adds a second layer:

- **Team layer:** everyone needs the same customer/job to succeed and protect the shared wallet.
- **Personal layer:** each barber wants visible credit, a good highlight, a clever rescue, or a funny title.

This should create disagreement, overconfidence and risky hero plays **without creating a reason to intentionally make the team lose**.

Do not add four shops, personal cash, classes, or a personal winner.

## 2. Narrative shell (lightweight, non-blocking)

Working fiction only; do not spend substantial art/content time on this in v0.4:

> A difficult/high-profile customer invites several of the city's most notorious barbers to solve one impossible commission together.

This explains why multiple barbers crowd one customer while still wanting to show off.

## 3. Gameplay Impact Ledger

Create one host-authoritative event stream for meaningful gameplay changes. Prefer adapting existing causal statistics and incident attribution rather than duplicating them.

Suggested logical record (names may match existing conventions instead):

```text
ImpactEvent
- EventId
- MatchId / JobIndex
- HostTime
- ActorPlayerId
- ToolId / ActionKind
- TargetObject/Region (coarse; no strand-level identity required)
- QualityBefore
- QualityAfter
- QualityDelta
- IncidentSeverityDelta
- SharedMoneyDelta (if any)
- CrisisId? / RescueId?
- Context flags (lateRound, dangerousTool, customerReacting, validatorApproaching, etc.)
```

Do not network every ledger event to all clients during build unless presentation requires it. The host may retain the detailed ledger and send result summaries/highlight facts later.

## 4. Live goal quality

The ledger needs one normalized live value representing "how healthy is the current job right now?"

Do **not** create a second scoring model that disagrees with validation. Expose/reuse current validator/goal logic to produce a cheap live evaluation.

Recommended conceptual range: `0..100`.

For Helipad this can derive from existing real shell contact/area/flatness/stiffness/heat checks. Other goals may approximate functional readiness until final validation.

The live quality is an instrumentation signal, not the final payout formula.

## 5. Action-window aggregation

Do not emit one event per frame for continuous tools.

Aggregate meaningful actions into windows:

- discrete tool action: one event;
- continuous spray/blower/fire: aggregate roughly 0.4–0.75 seconds or until release/target change;
- sculpt drag: aggregate the existing stroke/gesture, not every lattice edit;
- customer reaction by itself is environmental context, not a player action, unless it is causally attributed to a player's recent dangerous action using existing incident attribution.

For each window:
1. snapshot live quality at start;
2. apply authoritative gameplay normally;
3. after a short settle/quiet interval, snapshot quality again;
4. record the net delta and incidents.

Do not delay actual gameplay for measurement.

## 6. Positive and negative impact

For post-job statistics:

```text
PositiveImpact += max(0, QualityAfter - QualityBefore)
NegativeImpact += max(0, QualityBefore - QualityAfter)
```

These totals are **descriptive**. They do not make a personal winner and do not change team money.

Avoid rewarding meaningless oscillation:
- coalesce repeated micro-edits within the same action window;
- ignore tiny quality deltas below a tunable epsilon;
- do not count the same recovered quality repeatedly through rapid destroy/restore loops.

## 7. Crisis detection

Create a `Crisis` when one of these happens:

- live quality drops by a meaningful threshold in a short interval (initial default: about 12 points);
- a severe incident occurs (KO/fatal prototype incident/major fire/critical support loss); or
- a validator-specific critical condition flips from viable to non-viable late in the job.

A crisis stores:

```text
Crisis
- CrisisId
- StartTime
- PreCrisisQuality
- BottomQuality
- PrimaryCausePlayerId? (if attributable)
- CauseEventIds
- Resolved / Expired
```

Do not require every crisis to have a player cause. Environmental/customer/validator movement may create unattributed crises.

## 8. Rescue detection

A rescue is measurable recovery from a crisis, not an inferred heroic intention.

Initial prototype rule:

- rescue window: ~8 seconds after crisis start / latest bottom;
- recovery is capped at the pre-crisis quality baseline;
- each player's positive impact within that recovery range receives proportional rescue credit;
- positive progress above the old baseline is ordinary contribution, not extra rescue farming;
- remaining <=15 seconds may apply a modest urgency multiplier for highlight ranking only.

### Anti-farming rule

If player X is the primary cause of a crisis, player X receives **zero Rescue credit for restoring the portion of damage they caused** during that crisis.

They may still receive ordinary positive impact if they improve the job beyond the pre-crisis baseline later. The purpose is not punishment; it prevents "break it, fix it, farm hero title" behavior.

## 9. Surviving impact

Immediate improvement can disappear a second later. Awards should prefer improvements that survive.

Implement the lightest robust form:

- mark positive impact provisional;
- confirm/reduce it after ~2 seconds, at submission/timeout, or when replaced by a later destructive action;
- preserve enough information to distinguish "made it better for one frame" from "made a useful improvement that stayed".

Do not attempt academic Shapley-value attribution. Approximate, explainable results are better for this party game.

## 10. Attribution scope

v0.4 does **not** require full voxel/cell ownership history.

Reuse current source/incident facts first. If existing hair region metadata already stores source attribution cheaply, use it where useful, but do not add expensive per-sample provenance solely for awards.

Potential future v0.5 extension:
- creator / last sculptor / state applier provenance on coarse regions;
- final validation credit distribution based on surviving regions.

This is optional for v0.4.

## 11. Highlight integration

Use the same ledger to rank replay candidates.

Good highlight candidate signals:
- largest negative impact / accident;
- biggest rescue swing;
- high urgency rescue in final 15s;
- one action affects multiple players/customer/tool/goal facts;
- dangerous tool produces strong positive impact;
- customer reaction directly precedes an attributed accident;
- shared-money special-tool order immediately leads to a dramatic success/failure.

Select a bounded replay window around the winning event using the existing replay recorder. Do not build a second full replay system.
