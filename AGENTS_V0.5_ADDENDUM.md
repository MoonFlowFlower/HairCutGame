# AGENTS v0.5 Addendum — Hair Material & Tool Grammar

This addendum supplements the repository's existing `AGENTS.md`. Existing project rules remain binding unless this file explicitly supersedes a sculpt/material/target-reference rule.

## 1. Preserve the working prototype

This is a continuation of an existing Godot 4.7.2 Mono/.NET C# prototype. Read the latest `PROJECT_STATE.md` before editing.

Do not restart the project, replace the networking model, replace the authoritative continuous hair volume, or discard working v0.3/v0.4 systems merely to make this feature set easier.

Reuse current:
- host-authoritative multiplayer,
- shared-head session flow,
- continuous low-poly hair shell / volume editing,
- coarse material metadata,
- player/customer hair ownership,
- tool framework,
- scoring/validators,
- replay/impact attribution if already present,
- bilingual UI infrastructure,
- automated tests and launch/smoke scripts.

## 2. New product principle

Hair is no longer merely a target shape. Treat it as a **low-poly, plastic, semi-soft material with readable physical states**.

A player looking at the head with ordinary HUD hidden should be able to infer, approximately:
- which region is normal,
- newly grown/young and soft,
- wet/heavy,
- frozen/rigid/brittle,
- glued/fused,
- burnt/charred.

Do not expose hidden numerical stats as the main communication method. The world must communicate state through appearance, motion, sound, particles and tool response.

## 3. Secondary motion, not a full soft-body rewrite

Keep authoritative gameplay geometry as the existing solid sculptable volume.

Add a bounded **secondary visual motion layer** for small inertial/Q-like movement. It may be driven by coarse regions/springs, deformation controls, vertex offsets, or another robust low-cost representation. Choose the smallest convincing method that:
- makes normal hair visibly elastic but quickly damped,
- makes young hair softer and livelier,
- makes wet hair heavier and less bouncy,
- makes frozen/glued/burnt states visibly less elastic in distinct ways,
- does not make precision sculpting feel like working on uncontrolled jelly.

Small visual wobble need not mutate authoritative volume. Sustained player forces that intentionally reshape hair still must mutate authoritative gameplay geometry through the existing host-authoritative path.

Do not network per-vertex secondary motion. Synchronize the authoritative shape/material facts and derive small visual motion locally.

## 4. Expand the gameplay grammar

The core hair verbs are:

**Add / Remove / Move / Organize / Sever / Join / Set / Change-State**.

No single ordinary tool should dominate three or more of the following categories: shape creation, spatial relocation, material-state change, structural fixation.

Differentiate tools through physical operation and natural constraints, not primarily cooldowns, random spread or poor controls.

## 5. Sculpting reliability is non-negotiable

Fun difficulty should come from material/tool consequences, not uncertainty about what a click will do.

For precision tools:
- preview the expected result before commitment,
- keep tool orientation and effect orientation consistent,
- use light stabilization/snap assistance only where it preserves manual agency,
- provide distinct contact/audio/debris feedback for no-contact, shallow contact and heavy contact.

## 6. Diegetic target reference

Each player gets access to a personal **off-hand reference board** for the current job.

The board is not a shared scarce resource and must not consume the primary tool slot. It should present:
- front orthographic view,
- side orthographic view,
- top orthographic view,
- a small number of material/functional cues where required.

Use the same real target definition/model/material-state data used by scoring. Do not hand-author a misleading approximation.

A shared **miniature target maquette** also exists physically at a target/reference station in the shop. It is anchored/protected for v0.5 so the team cannot lose the only 3D reference. Players can walk around it and inspect it from arbitrary angles.

The current spatial target ghost/outline becomes an optional assist, not the primary visual language.

## 7. Required target props exist before validation

Goal props are not magic validation-only decorations.

Examples:
- Bird Nest: eggs are stocked or delivered during the build and players physically place them.
- Clothesline: underwear exists as world props and players physically hang it.
- Rocket silo: the rocket exists before validation and is physically inserted/positioned by players.
- Hair bridge: the test vehicle exists in the shop/staging area before validation.
- Cat goal: the cat exists as a world actor before validation; validation may ask it to climb/use the built structure.
- Helicopter: the RC helicopter is pre-staged in the world before validation; validation transitions that same entity into approach/landing rather than spawning a new one.

At validation time, the game inspects/activates the **same entity IDs and placements** established during construction. If they are missing, misplaced, destroyed or knocked away, that matters.

Props can come from:
- stocked shop inventory,
- commission-supplied crates,
- player orders from the shared-wallet tool/prop terminal.

If a required prop costs money, goal selection/offer logic must prevent an unwinnable state where the required prop cannot be obtained.

## 8. Autonomy

Do not ask the user routine architecture questions. For reversible implementation choices, choose the smallest robust solution consistent with these docs, record material decisions in `PROJECT_STATE.md`, test it, and continue.

Do not stop after one milestone. Continue through the v0.5 acceptance plan as far as the environment allows.
