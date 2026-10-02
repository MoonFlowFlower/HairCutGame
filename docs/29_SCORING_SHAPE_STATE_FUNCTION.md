# Scoring — Shape + Material State + Function

## Why

If scoring rewards only geometric overlap with a target volume, precision clipper + growth spray can remain sufficient for nearly every commission.

v0.5 adds target features that require visible material states and real functional behavior.

## Goal definition layers

A goal may define:

### Shape features
Existing target-volume / contact / flatness / cavity logic.

### Material-state zones
Target regions/volumes that prefer or require a state, e.g.:
- charred rim,
- wet interior,
- frozen support,
- glued joint,
- soft/young cushioning zone.

Do not prescribe a specific tool ID in the score. Score the resulting state. Today that state may only be producible by one tool; architecture should permit future alternatives.

### Functional checks
Real validation with existing world props:
- landing dwell,
- egg support/containment,
- rocket clearance/launch,
- bridge traversal,
- cat climb/support,
- clothesline wind resistance.

## Recommended weighting

Do not globally hard-code one split for every job. Each goal chooses weights within sane bounds.

Typical ranges:
- Shape: 45–70%
- Material state: 10–30%
- Function: 20–40%

The final team result should remain understandable. Present at most a few major components to players; detailed submetrics can remain internal/debug.

## Examples

### Helipad
- Shape 60
- Stability/material 15
- Landing function 25

### Charred Cake
- Shape 55
- Correct char distribution 25
- Candle/prop stability 20

### Bird Nest
- Shape/cavity 45
- Softness/support suitability 15
- 3 real eggs retained through validation 40

### Rocket Silo
- Shape/straight bore 50
- required wall state if applicable 15
- actual staged rocket launches without collision 35

## Anti-CAD principle

A visually imperfect but functional structure can score well.
A near-perfect target-volume trace that fails its real-world purpose should not receive an excellent overall result.

This preserves creative solutions and keeps the game from becoming exact 3D tracing.
