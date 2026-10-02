# Tool Grammar & Differentiation

## Core verbs

1. **Add** — create material where missing.
2. **Remove** — safely shave excess surface material.
3. **Move** — reposition existing mass without creating/deleting it.
4. **Organize** — align/smooth flow or surface without large volume change.
5. **Sever** — change connectivity / cut through a structure.
6. **Join** — connect previously separate/contacting pieces.
7. **Set** — hold a useful shape against forces.
8. **Change State** — alter wetness, heat, stiffness, brittleness, etc.

A tool should have a clear primary verb and at most a small number of secondary consequences.

## Natural constraints over artificial nerfs

Prefer:
- geometry of the tool,
- push vs pull direction,
- material conservation,
- proximity,
- commitment/irreversibility,
- state compatibility,
- world collateral,
- finite physical reservoir/ammo,

over:
- random spread,
- hidden miss chance,
- arbitrary long cooldowns,
- deliberately bad mouse control.

## Core tool identities

### Precision Clipper — shallow local Remove
Strength:
- controlled finishing,
- trimming bumps/edges,
- chamfers and local cleanup.

Restriction:
- acts on a shallow visible surface layer per stroke,
- poor at deep cavities, large-volume removal or through-holes,
- repeated passes are needed for deep excavation.

Must provide a clear pre-contact removal preview.

### Growth Spray — directional Add
Strength:
- adds volume from an existing surface,
- fills deficits and builds height/thickness.

Restriction:
- growth is an extrusion along surface normal / hair-flow direction, not arbitrary 3D blob placement,
- newly created material starts in Young/soft state,
- it is poor immediate load-bearing material until stabilized.

Do not make growth inaccurate to balance it.

### Industrial Blower — Push / Organize
Strength:
- moves existing soft/young/wet material,
- presses a high side down,
- bends or aligns soft mass,
- affects spray mist, flame direction, loose props and players.

Restriction:
- primarily pushes away; cannot pull,
- force falls off/spreads with distance,
- frozen/glued regions resist it strongly,
- excessive force can disturb props and teammates.

Volume should remain approximately conserved for intentional shape movement.

### Vacuum Hair Cannon — Pull / Transfer
Strength:
- pulls material toward nozzle,
- can reshape without deleting material,
- continued suction can detach/collect material,
- stored material can be redeposited.

Restriction:
- reservoir conserves material,
- empty reservoir cannot fabricate hair,
- full reservoir limits collection,
- pulling attached player hair may tug the player.

### Hedge Trimmer — Large Plane Cut / Sever
Strength:
- instantly establishes a broad plane or step,
- fast platform/block construction.

Restriction:
- bad at curves, holes and tiny detail,
- long physical blade occupies world space and can hit customer/player/props,
- can sever a supporting neck and drop the entire disconnected mass.

Not simply a faster clipper; its spatial effect is a plane/blade.

### Glue Gun — Join / permanent-ish Set
Strength:
- bonds touching hair parts or hair-to-anchor contacts,
- excellent for bridge ends, supports, attached props.

Restriction:
- committed glued regions become difficult to reposition,
- visible glue remains as evidence,
- not brittle like frozen state,
- heat/cutting may be needed to undo mistakes.

### Sniper / Precision Puncture Tool — Puncture / Through-cut
Strength:
- narrow, round-ish, deep/through tunnel,
- rocket silo, bullet-hole, bore-type geometry.

Restriction:
- penetration continues beyond the intended hair surface,
- customer/player/world behind the target are at risk,
- limited ammo or expensive use is acceptable,
- should not be the preferred tool for arbitrary surface shaving.

### Flamethrower — Heat + irregular Erode + Char
Strength:
- rapid area erosion,
- intentionally creates charred state,
- can melt/weaken compatible glue and thaw frozen regions if current material rules allow.

Restriction:
- irregular boundary compared with plane cutting,
- spreads heat/fire,
- dry/young material may ignite quickly; wet material resists,
- collateral is systemic, not a random penalty.

## Candidate next-wave tools (not required for first v0.5 completion)

### Water Spray — Wet state
- makes hair heavier/softer, reduces ignition,
- creates wet floor/slip interactions.

### Salon Dryer — Dry + Set Flow
- lower force than blower,
- dries wet hair while gradually fixing the current flow direction.

### Liquid Nitrogen — Fast temporary Set / brittle Change-State
- immediately rigid,
- great for machining,
- breaks under impact.

### Hair Clamp / Clip — localized Mask analogue
- fixes a local region against push/pull,
- can be knocked/cut off,
- encourages natural multiplayer coordination.

### Hair Press — Compress
- flattens/thins a region without simply deleting all volume,
- creates dense stable plates,
- comically dangerous around the customer.

### Hair Extruder / "Noodle Machine" — regularize + stretch
- consumes existing hair mass and outputs a long regular beam/strand-like solid,
- useful for poles/bridges/clotheslines,
- obeys mass conservation and produces flexible thin structures.

## Anti-lock rule

Do not design a simple one-tool key for every target. Each common shape problem should normally admit multiple approaches with different time/risk/material consequences.

Example: uneven platform can be fixed by removing high material, adding low material, pushing high material, pulling low material, large-plane recut, or wet/press workflows.
