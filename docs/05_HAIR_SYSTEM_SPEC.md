# 05 — Hair System Spec

## Design goal

Hair must be a **toy/material**, not a beauty effect.

Priorities:
1. readable interaction,
2. strong transformation,
3. multi-tool composition,
4. networkable game facts,
5. low-poly comedic appearance.

Do not implement realistic strand hair.

## Representation

The user's solid-mass correction (2026-09-26) supersedes the original tuft representation.

- Hair is one continuous sculpted volume, rendered only as a merged, faceted outer shell.
- Never draw logical patches as separate spikes, strips, prisms, strands or bundles on the scalp.
- Cutting shaves or slices the volume, growth expands it locally, puncture subtracts a tunnel, burning erodes it, and freezing changes material/state without changing the underlying solid silhouette.
- The prototype uses a quantized signed-density lattice and a closed triangulated isosurface. Internal lattice cells are hidden implementation units, never rendered cubes.
- 32 scalp control regions remain for heat/glue/stiffness/attribution and coarse deformation; their old length/direction fields are compatibility controls, not the geometry or scoring truth.
- Authoritative volume samples also drive tool hits, mass accounting, scoring and validation support/clearance. Network the density facts, not rendered vertices.

## Suggested HairPatch state

Conceptual fields:

```text
HairPatchId
HairHeadId
AnchorLocalPosition
AnchorLocalNormal

Mass / Volume
Length
Direction / Bend
Stiffness
Brittleness
Temperature
GlueAmount
BurnAmount
Color

IsDetached
IsWigHair
AttachedEntityId?
ExternalAnchorId?
SourceOwnerId?
```

Not every field must be networked separately. Pack/quantize where practical.

Clamp all numeric state.

## States are composable

Avoid a single exclusive enum such as `Frozen OR Burning OR Glued`.

Useful combinations should exist:
- glued + burning,
- frozen + glued,
- wig + frozen,
- transferred + burning.

Use independent values/flags where practical.

## Core operations

HairSystem should expose operations equivalent to:

### AddHair
Increase mass/length locally.

### RemoveHair
Reduce mass/length. If almost zero, patch becomes visually bald.

### ApplyForce
Affect direction/bend and cosmetic simulation.

### ChangeTemperature
Can drive:
- freezing,
- thawing,
- ignition,
- burning.

### ChangeStiffness
Used by glue/freeze.

### Glue
Join patches/objects or increase resistance to deformation.

### Anchor
Attach a patch/control region to world/prop/other entity.

### Detach
Release hair from scalp/anchor, creating transferable hair object where appropriate.

### Transfer
Move detached/removed hair mass from storage to another head/object.

### CutPlane
Efficiently trim all intersecting hair against a plane/volume.

### Puncture
Remove a narrow path/volume along a ray, with possible continuation beyond first target.

### Ignite
Begin burn/spread behavior.

## Approximate physical behavior

Hair should feel flexible but not chaotic.

### Normal
- gently returns toward shaped/rest orientation,
- moves under strong force,
- does not collapse from tiny contact.

### Glued
- higher stiffness,
- stronger links/anchors,
- harder to reshape,
- can melt/burn depending on temperature rules if implemented.

### Frozen
- very high stiffness,
- increased brittleness,
- large impact can fracture/detach a chunk,
- ordinary wind should not instantly fracture it.

### Burning
- visual flame/smoke placeholder,
- loses mass over time,
- can spread to nearby hair depending on distance/heat,
- can interact with airflow.

## Detachment and transferable hair

Detached hair should become a reusable `HairObject`/bundle:
- can be vacuumed,
- can be attached as a wig/patch,
- can burn/freeze,
- can collide at simplified fidelity.

Do not create hundreds of debris bodies. Merge nearby detached hair into coarse bundles.

## Wigs

A wig is a hair-bearing object:
- contains pre-shaped hair patches,
- can attach to a head,
- can detach,
- can be cut/grown/burned/frozen/glued,
- can be blown/vacuumed away if not secured.

Prototype wig shapes:
- giant afro,
- long block of hair,
- flat-top slab,
- twin-tail shape.

Keep these generated/simple.

## Player hair

Barber avatars use a `HairHead` compatible with customers.

Difference:
- not scored in the prototype,
- persists across the 3-round match,
- resets only when the match resets.

First-person presentation requirements:
- remote players visibly see barber hair changes,
- own hair may become visible through salon mirrors,
- extreme front/top growth may intrude slightly into the local camera view for comedy/readability,
- smoke/frost/fire from own hair may have restrained first-person VFX,
- do not let own-hair presentation permanently blind the player or make basic control impossible.

## Scoring samples

HairSystem should be able to provide a stable sample set in head-local coordinates for scoring.

Possible method:
- sample occupied volume on a stable coarse lattice,
- avoid using render-triangle count as gameplay truth,
- target ~hundreds of samples per head, not tens of thousands.

## Networking

Network authoritative logical state/events.

Do not network:
- every visual segment transform at 60 Hz.

Clients may simulate cosmetic bend/sway locally from:
- patch direction,
- stiffness,
- current force hints,
- state flags.

Use occasional host reconciliation snapshots.

## Test hooks

HairSystem needs:
- reset head,
- set deterministic seed,
- dump patch state,
- force specific state,
- spawn wig,
- transfer known amount,
- score sample visualization.

These should be callable from `TestHairLab`.


## Facial hair extension (user playtest revision, 2026-09-26)

Each customer and barber also has left/right brow and beard volumes using the same effects, ray queries and renderer. Fine facial volumes use scaled lattice space; their geometry is independent of the scalp so they can be shaved bald and regrown. Position/orientation follows the parent head. Customer facial hair resets each round; barber facial hair persists. Submission locks every region. Scalp commission scoring excludes facial regions. Mirrors and replay include them.

## Sculpting and persistent clippings revision (2026-09-27)

See [17_SCULPTING_AND_PERSISTENT_CLIPPINGS.md](17_SCULPTING_AND_PERSISTENT_CLIPPINGS.md) for the implemented user-approved override: locked shallow clipper planes, time-based spray, normal/fine/size controls, tetrahedral surface/connectivity queries, persistent falling material, vacuum/blower cleanup, and host-authoritative accumulation with historical replay.

