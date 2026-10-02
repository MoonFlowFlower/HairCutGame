# 11 — Continuous Implementation / Migration Plan

Execute sequentially without asking for approval between milestones unless genuinely blocked.

## Milestone M0 — Audit current repo and establish migration map
- inspect existing Godot/C# project,
- build current version before changing it if possible,
- locate current player, network, hair, tool, station, goal, scoring, replay systems,
- create/update `PROJECT_STATE.md`,
- list reusable components and obsolete assumptions,
- preserve a clean git checkpoint if git is available (do not erase user work).

Exit:
- current architecture understood,
- build command known,
- migration plan recorded.

## Milestone M1 — Shared central customer
- create/convert salon to one central active chair,
- spawn one authoritative customer/head,
- allow all connected players to approach it,
- remove/bypass four independent customer ownership from main flow,
- preserve working first-person controllers and tool pickup.

Exit:
- 1–4 players can stand around and edit the same head.

## Milestone M2 — Shared round + vote + result
- replace per-player goal selection with shared 3-option vote,
- implement 75s shared timer,
- remove early individual submit/protection from main mode,
- implement shared result/shop money,
- basic helipad target preview/evaluator.

Exit:
- complete solo/shared round works without validator polish.

## Milestone M3 — Shared-head tool core
Ensure/refine:
- clippers,
- growth spray,
- blower,
- vacuum transfer,
- glue,
- hedge trimmer.

Make tool effects apply to compatible world targets rather than only isolated hair.

Exit:
- six tools produce clearly different useful/collateral behavior.

## Milestone M4 — Customer panic and micro-reactions
- implement hidden panic,
- add stimuli,
- add anticipation,
- implement at least duck + look/recoil,
- ensure actual head collision follows motion,
- authoritative reaction sync.

Exit:
- customer movement can cause real but readable misses without becoming constant sabotage.

## Milestone M5 — Player/world physical consequence
- blower pushes players/loose objects,
- suction tug through player hair or equivalent,
- large tools have meaningful spatial extent,
- refine crowding around central chair.

Exit:
- at least two natural player-to-player accident chains possible without dedicated attack buttons.

## Milestone M6 — Helipad validator
- tiny helicopter approach,
- rotor/downwash,
- landing attempt,
- understandable success/failure,
- shared result screen.

Exit:
- full intended 30-second climax exists.

## Milestone M7 — Network stabilization
- host + 1 client smoke test,
- host + 3 if machine permits,
- fix reaction/head state divergence,
- bounded logical state replication,
- disconnect handling.

Exit:
- stable enough for human multiplayer playtest.

## Milestone M8 — Stretch content if core is stable
In priority order:
1. sniper precision puncture,
2. flamethrower,
3. Bird Nest goal,
4. Llama customer + spit behavior,
5. incident highlight replay.

Do not sacrifice M1–M7 quality to chase breadth.

## Final stabilization
- run automated tests,
- run solo full round,
- run host+client,
- attempt 4 local instances,
- update run instructions and `PROJECT_STATE.md`,
- report only verified status/gaps.
