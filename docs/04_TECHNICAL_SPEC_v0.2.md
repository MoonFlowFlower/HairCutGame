# 04 — Technical Spec v0.2

## Engine/runtime

- Godot 4.7.2 stable .NET
- C#
- Desktop PC prototype
- Godot physics
- Godot High-Level Multiplayer / ENet

## Repository structure

Preferred starting layout:

```text
/
  AGENTS.md
  PROJECT_STATE.md
  project.godot
  ProjectHairball.csproj
  src/
    Bootstrap/
    Core/
    Game/
    Player/
    Customer/
    Hair/
    Tools/
    Goals/
    Scoring/
    Net/
    Replay/
    UI/
    Presentation/
  scenes/
    Main.tscn
    Salon.tscn
    Player.tscn
    Customer.tscn
    TestHairLab.tscn
    MultiplayerSmoke.tscn
  data/
    goals/
    tools/
  tests/
    Core/
    Integration/
  scripts/
    build.*
    test.*
    run_host.*
    run_client.*
    run_4p_local.*
  docs/
```

Exact folder names may change if there is a concrete reason. Keep conceptual separation.

## Boot flow

Recommended:

`Main -> AppBootstrap -> SessionMenu -> SalonSession`

Modes:
- Solo
- Host
- Join by IP/port

Prototype default port: 7777 unless occupied/configured.

## Main runtime systems

### SessionManager
Owns:
- offline/host/client setup,
- player registrations,
- peer disconnect handling,
- session seed.

### MatchManager
Owns:
- 3-round lifecycle,
- score totals,
- round transitions.

### RoundManager
State machine:
- Setup
- GoalChoice
- Build
- Lock
- Validation
- Results
- Highlight
- Complete

All state transitions authoritative on host.

### PlayerController
Owns:
- first-person movement,
- mouse/gamepad look,
- aim/query origin,
- context interaction,
- one held primary tool,
- drop/reload/secondary input routing,
- submit interaction,
- barber hair head,
- simple body collider.

Normal gameplay camera is first-person. Third-person cameras are reserved for replay/spectator presentation. Keep input routing generic so tools share primary/secondary actions rather than defining bespoke control schemes.

### Customer
Owns:
- assigned player,
- seated state,
- reaction state,
- health/incident proxy,
- ragdoll/recovery,
- customer hair head.

### HairSystem
Owns authoritative hair facts.

See `05_HAIR_SYSTEM_SPEC.md`.

### ToolSystem
Converts player input into generic effects.

See `06_TOOL_INTERACTION_SPEC.md`.

### GoalSystem
Owns:
- goal definitions,
- 3-choice generation,
- target visualization,
- final validation.

### ScoreSystem
Pure-ish deterministic scoring where possible.

### IncidentSystem
Tracks:
- tool source player,
- victim entity,
- severity,
- ownership relation,
- penalties,
- replay event.

### ReplayRecorder
Rolling snapshots/events after core gameplay is stable.

## Pure logic vs Godot-bound logic

Keep math/state that can be tested without engine dependencies in plain C# where practical:

Examples:
- round state transitions,
- score composition,
- incident attribution,
- goal selection from difficulty bands,
- hair effect arithmetic/state transitions,
- highlight event ranking.

Godot-bound:
- nodes,
- physics queries,
- rendering,
- ragdolls,
- input,
- networking integration.

This enables fast `dotnet test` coverage.

## IDs

Do not rely on scene tree paths as authoritative identity.

Use stable runtime IDs:
- `PlayerId`
- `EntityId`
- `HairHeadId`
- `HairPatchId`
- `ToolUseId`
- `RoundId`

Network messages/events reference IDs.

## Time/ticks

Suggested:
- physics: engine default 60 Hz,
- gameplay/tool request rate: cap/rate-limit continuous tools,
- player transform replication: moderate fixed rate + interpolation,
- hair fact replication: event-driven plus occasional reconciliation snapshot,
- replay snapshots: ~10 Hz is enough for prototype.

Tune after profiling. Do not network every hair visual control point every physics frame.

## Data definitions

Prefer C# resources or JSON-like Godot Resources that remain inspectable/text-friendly.

### ToolDefinition
Fields may include:
- id,
- display name,
- ammo/charge,
- use mode,
- range,
- effect bundle,
- friendly/collateral behavior,
- VFX presentation params.

### GoalDefinition
Fields may include:
- id,
- difficulty band,
- target SDF/volume composition,
- target preview,
- validation type,
- prop requirements,
- weight overrides.

## Placeholder generation

Create reusable procedural/simple generators for:
- low-poly barber/customer bodies,
- heads,
- chairs/stations,
- hair,
- wigs,
- goal props,
- shop walls/floor,
- tool placeholder meshes.

Do not spend time hunting external assets.

## Debug tooling

Build a debug overlay toggled by a key:
- player/peer IDs,
- authority,
- current round state,
- current goal ID,
- own estimated shape score,
- incident count,
- network RTT if easy,
- selected hair patch under aim,
- hair state flags.

Build a `TestHairLab` scene for:
- one head,
- tool switching,
- reset,
- effect inspection,
- target visualization.

This scene should accelerate iteration.

## Error handling

Prototype should fail visibly rather than silently.

- log invalid effect applications,
- validate missing goal/tool definitions on boot,
- assert/guard invalid owner IDs,
- handle disconnected peer cleanly,
- clamp hair parameters,
- never allow NaN/Infinity transforms.

## Performance budget

Prototype target:
- 4 players,
- 8 heads total (4 barber + 4 customer),
- ~24–40 logical hair patches per head,
- enough visual segments for readable low-poly motion,
- stable desktop framerate on a normal development PC.

Prefer a small number of meaningful logical chunks over hundreds of physics bodies.


## Visual intent references

See `docs/reference/` and `docs/16_VISUAL_REFERENCE_GUIDE.md`. These images are not pixel-perfect art requirements. They define: first-person proximity, large readable customer heads, chunky low-poly deformable hair, absurd shared tools, neighboring stations visible in one salon, and readable physical chaos. Gameplay causality and interactability take priority over decorative fidelity.
