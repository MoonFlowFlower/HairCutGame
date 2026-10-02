# 33 — Imminent Validation and Temporary Bracing

## Goal

The helicopter is not an animation that appears after the work is done. It is the approaching physical deadline.

Players retain control throughout flyby, final approach, touchdown and stabilization.

## Recommended 75-second B timeline

Times below are **remaining time** and may be tuned slightly after internal fairness checks.

### T = 75s — Start
- Same physical RC helicopter entity exists in the world from the beginning.
- It is visible/audible outside or in a staging position.
- Players begin work.

### T = 45s — Low flyby / mini stress test
- Helicopter makes a close pass.
- Real downwash affects relevant gameplay objects.
- Soft/unsecured hair can bend/lift.
- loose light props can move;
- customer notices / wants to look.

This is a mid-round “you are not ready” moment while the team still has time to react.

### T = 20s — Final approach
- helicopter commits to the shop/chair approach path;
- audio and visual proximity increase;
- customer attention pressure toward helicopter/window increases;
- players still have full control.

### T = 10s — Descent begins
- landing gear is descending while work continues;
- the team may trim, brace, glue/freeze, blow, pull or otherwise intervene;
- do not switch to spectator mode.

### T = 0s — Latest forced touchdown resolution
- if no early touchdown has occurred, the helicopter makes the final attempt;
- players still retain control until the success/failure condition is physically resolved.

## Downwash

Use the existing force/tool/material framework where possible.

The flyby/final downwash should materially affect at least:

- soft / unsecured hair;
- loose cut material;
- loose/light props used in the test scene.

Do not use a purely cosmetic wind effect.

## Same entity rule

The helicopter used for the flyby/approach/landing is the **same pre-existing entity**, not a validation replacement spawned directly above the target.

## Temporary bracing

Any player near the customer may perform a temporary brace action.

Suggested prototype interaction:

- aim/stand near head or chair;
- hold `E` (or integrate with current interact semantics);
- while bracing, customer head reaction amplitude/velocity is substantially reduced;
- bracing player cannot actively use their held primary tool;
- they do not need to permanently drop it unless current code makes that substantially cleaner;
- bracing can be released at any time.

This is a transient need, not a class/job.

### Important

Bracing should not make the customer perfectly immobile. Very strong reactions or the helicopter’s attention pressure can still cause small movement or drag the brace slightly.

The aim is:

> “help me hold him for three seconds”

not:

> “Player 4 is the permanent head-holder.”

## Early-finish / call-helicopter button

Do **not** make early-call-for-more-money part of the first v0.6 comparison unless internal testing shows completed teams are regularly waiting with nothing meaningful to do.

If later added as B2, treat it as a separate experimental variable.
