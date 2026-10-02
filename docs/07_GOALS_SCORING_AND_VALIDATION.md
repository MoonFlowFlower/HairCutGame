# 07 — Shared Goals, Scoring & Validation

## Shared goal selection

At round start, present 3 shared goals of similar expected effort. All players vote during a short window. One goal is selected for the entire group.

Do not assign separate goals per player in this mode.

## Shared scoring philosophy

The important result is whether the team produced a convincing functional construction without causing excessive disaster.

Suggested result components:
- Shape / structural match: 0–100
- Functional validation bonus: 0–50
- Customer incident penalties
- Property/prop damage penalties
- Optional speed/cleanliness bonuses later

Convert to one shared money result.

Do not expose constantly updating exact score during active play if it encourages sterile optimization. A rough quality indicator/debug display is fine during development.

## Initial goals

### 1. Helicopter Helipad — first acceptance goal
Desired:
- broad flat top,
- sufficient usable area,
- reasonable stability/clearance.

Build-time last segment:
- helicopter sound/approach,
- rotor wash begins affecting flexible material/loose props.

Validation:
- tiny helicopter attempts landing,
- success if platform geometry/stability is sufficient for a short dwell.

### 2. Bird Nest
Desired:
- concave containment zone,
- surrounding support.

Validation:
- place 3 eggs,
- rotate chair / apply small disturbance,
- eggs must remain.

### 3. Hair Bridge
Desired:
- connected span across designated anchors/regions,
- sufficient path width/stiffness.

Validation:
- small toy vehicle crosses.

### 4. Cat Tree
Desired:
- tall trunk/support + top platform.

Validation:
- toy/animated cat climbs or jumps onto top and remains briefly.

### 5. Rocket Silo
Desired:
- clear central vertical channel.

Validation:
- small rocket fires through it without colliding catastrophically.

### 6. Underwear Clothesline
Desired:
- two supports with connected span.

Validation:
- hang lightweight underwear props,
- apply fan/wind test.

## Design rule for validators

Validator should physically touch/affect the construction. It should create anticipation.

Do not require expensive simulation when a simple deterministic proxy can create the same readable moment.
