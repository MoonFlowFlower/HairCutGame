# Hair Material Language

## Principle

Material state must be communicated redundantly through **appearance + motion + interaction feedback**. A pure color swap is insufficient.

The exact parameter values are implementation details. What matters is perceptual separation.

## Baseline states

### 1. Normal
Visual:
- existing low-poly base color,
- mostly matte with mild soft highlights,
- coherent chunky surface.

Motion:
- small elastic lag when the head accelerates,
- modest response to blower/vacuum,
- quickly damped; no endless jelly wobble.

Gameplay:
- standard sculptability,
- medium stiffness/elasticity.

### 2. Young / Newly Grown
Created primarily by growth spray.

Visual:
- slightly lighter/fresher tone or edge treatment,
- softer/fuzzier silhouette without exposing realistic strands,
- brief subtle post-growth quiver.

Motion:
- softer, higher amplitude response,
- easier to push/pull,
- more prone to sagging.

Gameplay:
- low stiffness,
- easy to reshape,
- poor immediate structural support,
- naturally stabilizes somewhat over time or after treatment.

Important: growth remains precise and pleasant. Its limitation is material character, not random inaccuracy.

### 3. Wet
Visual:
- darker color,
- tighter clumping/readable larger patches,
- sharper/specular wet highlight,
- occasional droplets only where useful.

Motion:
- heavier,
- less springy,
- stronger downward sag.

Gameplay:
- easier to press/comb into a lower shape,
- harder to ignite,
- may reduce glue effectiveness until dried.

### 4. Frozen
Visual:
- cold desaturation / pale frost,
- crystalline/frosted edges,
- sharper highlights that read as ice, **not metal**,
- optional tiny cracks after stress.

Motion:
- almost no Q motion.

Gameplay:
- high stiffness,
- high brittleness,
- strong impact can fracture/detach a whole piece.

### 5. Glued / Fused
Visual:
- transparent/translucent glossy glue bridges or films between surfaces,
- sticky strands/fillets at joins,
- fused clumps read as bonded rather than icy.

Motion:
- low local wobble at the bonded region.

Gameplay:
- strong connection/anchor,
- tough rather than brittle,
- difficult to reposition after commitment,
- heat may weaken/melt glue depending on current tool rules.

### 6. Burnt / Charred
Visual:
- brown -> dark brown -> black progression,
- lower/matte roughness in fully charred zones,
- curled/shrunken edge profile,
- ash flecks and occasional small ember glow while still hot,
- visible loss of clean soft silhouette.

Motion:
- little elastic response,
- dry and brittle.

Gameplay:
- continued heat erodes/shrinks material,
- impact or strong blower can shed brittle ash/chunks,
- state remains visually distinct after flame is gone.

## Continuous transitions

Avoid instant binary state changes where practical.

Examples:
- heating: normal -> warmed -> scorched -> charred -> eroded/removed,
- wetness: dry <-> damp <-> wet,
- freezing: normal/wet -> frosted -> rigid -> fractured under sufficient impact,
- young hair: fresh soft -> settling -> normal/stabilized.

The prototype does not require physically accurate thermodynamics. Transitions must be deterministic, bounded and readable.

## Secondary motion architecture requirement

Preserve the authoritative continuous volume as the scored/collidable shape.

Add a visual secondary-motion representation attached to coarse material regions. The implementation may use region springs, deformation controls, shader/vertex offsets or a small set of render control points.

Acceptance properties:
- normal region visibly lags and returns after a quick head turn,
- young region moves more,
- wet region droops/heavily follows,
- frozen region barely moves,
- glued region holds its join,
- burnt region barely springs and can shed brittle pieces under stress.

Do not create per-strand physics or per-vertex network replication.

## Readability test

Hair Lab must expose a six-state material test head or six comparable samples.

With debug labels hidden, a human should be able to correctly infer most of:
- soft/new,
- wet/heavy,
- frozen/hard,
- glued/fused,
- burnt/charred,
from ordinary gameplay camera distance.
