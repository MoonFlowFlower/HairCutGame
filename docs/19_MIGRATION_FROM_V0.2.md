# 19 — Migration From v0.2 Separate Stations to v0.3 Shared Head

This document is specifically for transforming an already-working v0.2 build.

## Product delta

### Old main mode
- up to four customers/stations,
- each player owns a customer/goal,
- per-player score race,
- players can sabotage neighboring work,
- early individual submit locks the result.

### New main mode
- ONE customer/head,
- all players work on it,
- one shared goal,
- shared shop payout,
- no individual submit,
- customer reactions and tool physics create interference,
- validators physically test the shared construction.

## Preserve where practical

Do not throw away working implementations of:
- first-person controller,
- network player spawn/ownership,
- world tool pickup/drop,
- tool base classes,
- generic HairEffect pipeline,
- HairChunk/HairSystem,
- tool VFX/audio,
- host-authoritative RPC plumbing,
- incident logging,
- replay/snapshot code,
- procedural low-poly art helpers.

## Refactor/remove

### Station ownership
Old: player -> station -> customer -> goal.

New:
`SharedRound -> ActiveCustomer -> SharedHeadMaterial`

Players should not require assigned customer ownership.

### Goals
Old: per-player independent choice.
New: shared three-option vote then one active goal.

### Score
Old: player score + opponent ranking.
New: shared job result/shop money + optional comic per-player statistics.

### Submit
Old: player can cash out/protect their station.
New: remove from primary shared mode.

### Scene
Old: four equal workstations/customer chairs.
New: central chair with 360-degree approach space. Reuse perimeter counters/tool racks where useful.

## Suggested safe migration sequence

1. Keep old mode temporarily behind an unused/debug scene if that lowers refactor risk.
2. Add `SharedRoundManager` alongside old round manager.
3. Spawn only one customer/head in new shared scene.
4. Let all players target it using existing tool/effect system.
5. Replace per-player goal UI with shared vote.
6. Replace scoring/result path.
7. Remove submit/protection calls from shared path.
8. Add customer panic/reactions.
9. Add validator.
10. Once shared mode passes acceptance, delete or archive unreachable obsolete code to reduce confusion.

## Anti-patterns

Do not:
- copy four station systems and simply point them all at one customer,
- keep hidden per-player scoring as the real objective,
- keep station locks that block teammates from editing parts of the same head,
- preserve old architecture so aggressively that every shared action requires fake ownership workarounds,
- rebuild hair/network/tool systems from scratch without evidence they are unusable.

## Regression search terms

Search code/scenes/docs for concepts like:
- `PlayerStation`
- `AssignedCustomer`
- `SubmitRound`
- `SubmittedStation`
- `OpponentScore`
- `StationScore`
- four-customer spawn loops

Names vary; inspect behavior, not just literal strings.
