# 04 — Technical Spec v0.3

## Engine
- Godot 4.7.2 stable Mono/.NET
- C#
- Windows primary
- ENet + Godot High-Level Multiplayer for prototype
- Host authoritative

## Migration-first architecture

Do not assume a blank repo. Inspect the current v0.2 build and adapt existing systems. Prefer facades/adapters around working code over a wholesale rewrite, but remove obsolete station ownership once the shared flow works.

## Suggested module boundaries

### Game / Round
- `GameSession`
- `SharedRoundManager`
- `GoalVoteManager`
- `SharedShopWallet`
- `RoundResultService`

### Players
- `PlayerCharacter`
- `FirstPersonController`
- `ToolHolder`
- `PlayerHairComponent`
- `PlayerImpactController`

### Customer
- `CustomerController`
- `CustomerBehaviorProfile`
- `CustomerPanicController`
- `CustomerReactionController`
- `CustomerHeadAnchor`

### Head material
- `HeadMaterialSystem` or existing `HairSystem`
- `HeadMaterialPatch` / existing `HairChunk`
- `HeadMaterialState`
- `DetachedMaterialBundle`

Keep existing Hair naming if a rename would add risk; introduce interfaces/abstractions at boundaries instead of churn.

### Tools
- `ToolBase`
- `ToolActionRequest`
- `EffectResolver`
- compact effect structs/classes
- world-target adapters for hair/material, player body, props, customer panic stimulus

### Goals / validation
- `GoalDefinition`
- `GoalEvaluator`
- `ValidationController`
- `HelipadGoal`
- `HelicopterValidator`

### Networking
- `NetworkSession`
- `NetworkActionRouter`
- authoritative effect resolution
- compact state/event replication

### Incidents / replay
- `IncidentEvent`
- `IncidentRecorder`
- optional snapshot ring buffer

## Shared-head authority model

Client sends intent:
- use tool,
- tool pose/origin/direction,
- interact/pickup/drop,
- vote goal.

Host validates/resolves:
- valid held tool,
- hit/query,
- affected entities,
- head-material modifications,
- panic stimulus,
- player impulse/damage state,
- prop state,
- validation state,
- money/result.

Host broadcasts compact facts/events.

Do not network every visual hair tuft transform each frame. Replicate logical patch state/control points/parameters at bounded rates; cosmetic jiggle/debris can be local and corrected from authoritative state.

## Head material representation

Prototype target: ~24–40 logical regions on the baseline human head, or an equivalent chunk count that maintains clear silhouette editing.

Each logical patch may include:
- anchor/scalp region,
- amount/length/volume,
- direction/control orientation,
- stiffness,
- wetness,
- temperature/burning state,
- glue/attachment links,
- material type,
- visual seed.

Renderer may use multiple low-poly pieces per logical patch without multiplying network state.

## Customer reaction mechanics

Do not move only the visual mesh while leaving tool collision in place. Gameplay-relevant head motion must move the collision/interaction target consistently.

Recommended approach:
- seated root/body anchor,
- head/neck transform controlled by reaction animation/state,
- authoritative reaction trigger and phase,
- clients reproduce the same parameterized motion.

Avoid full networked ragdoll for baseline reactions.

## Player body physics

Use controlled character movement with externally applied impulses. Full free ragdoll is optional/brief.

Need enough physicality for:
- blower shove,
- collision/crowding,
- tool sweep contact,
- suction tug.

Do not make movement so unstable that precise head interaction becomes frustrating.

## Goal evaluation

Helipad evaluator can combine:
- target area occupancy,
- top flatness/height variance,
- minimum usable area,
- optional central marker/clearance proxy.

Physical helicopter validation then tests stability/clearance with rotor/downwash.

The functional validator should not require perfect simulation; use the smallest convincing approximation.

## Scene concept

Primary gameplay scene:
- one central chair/round work zone,
- enough 360-degree approach space for 4 players,
- tool racks around perimeter,
- validation prop staging area,
- mirrors for player hair feedback,
- uncluttered collision near central head.

Follow reference 03 for intent, not exact geometry.

## Data-driven customer profiles

Customer profile fields may include:
- display id,
- model/visual resource,
- base head material type,
- panic sensitivity modifiers,
- signature reaction behavior,
- allowed goals/compatibility.

Keep signature behavior modular so a llama can add spit without rewriting base panic logic.

## Testing seams

Pure C# where practical for:
- goal vote resolution,
- panic accumulation/cooldown,
- effect composition,
- score/money calculation,
- incident ranking,
- head target scoring math.
