# Underwear clothesline: live comedy staging

Implemented 2026-09-27. This is a playable first-person scene and a presentation update to normal goal 6, not a painted screenshot.

## Play

```powershell
./scripts/build.ps1
./scripts/run_solo.ps1 -Laundry
```

The main menu also has a bilingual Laundry Lab button. The lab starts with an asymmetric, slightly sagging solid hair frame, a narrowed right support, partial glue repair and a held glue gun. It has unlimited practice time. F5 restores the fixture and player position. Normal commissions still start with ordinary hair and retain the existing scoring rules and timer.

LMB uses the held tool; E swaps physical tools; G drops; Space jumps; aim at the blue chair control and hold R/F to adjust height. Growth spray, blower and trimmer are on the floor to the customer's right. Other tools remain on the benches. Hold Tab to inspect the actual view-dependent target silhouette. Esc exposes language/reset/menu controls; F3 restores full diagnostics. Chinese/English preferences continue to persist.

## What changed

- Three patterned underwear props are visible during building, with pegs and short straps. Their contacts are found on the actual extracted hair surface. A missing beam makes them hang awkwardly at the head instead of inventing invisible support. One garment hangs lower. Final validation still uses authoritative validation props and scoring.
- Cloth swings and flaps; nearby active blowers amplify it. Head load motion is deliberately small, with stronger acting when supports are missing. Customers look up, blink, sweat and open their mouths under stress.
- Glue strings, contact blobs, green growth puffs and wind streaks originate at the tool. They use authoritative tool-use/contact facts, shared by snapshots and replay. The pool is fixed at 20 puffs and three string segments per station.
- The glue gun has a glue stick, nozzle, grip and side panels. First-person tool placement avoids filling the view with the rear of the gun. Tool animations remain subordinate to aiming.
- The lab includes a real loose purple wig and existing persistent floor clippings. A small animated fan and glue-stick tray provide secondary context. Repaired attachment points show glue blobs.
- Build HUD is compact: round/timer or lab context, commission, tool/charge, interaction prompts and small own-hair portrait. Nearby world tool labels appear on aim. The laundry target appears while holding Tab so it does not cover the joke.

## Presentation versus gameplay

**Presentation:** simplified underwear swing/straps, expressions, decorative fan, repair blobs, tool puffs/strings, hand pose, compact UI. Garments do not add collision, material, score or artificial support. The nearby decorative fan supplies a small visual ambient flutter; it is not another hidden damaging blower. Cloth is procedural animation, not a fabric solver. The lab beam's sag is authored volume geometry, not a new structural solver.

**Gameplay-affecting:** host load motion changes the customer head pose slightly; hair queries, visible head and facial hair use the same pose. The opt-in lab seeds actual editable volume, partial glue, physical tool possession, loose wig and clipping material. These remain usable by existing tools. Ordinary glue stiffening, growth bulging, blower deformation, breakage and clipping transport remain the actual simulation. Holding the industrial blower can destroy the frame; no presentation-only rescue prevents this. Automatic weight-based collapse and new hygiene/garment scores are not introduced.

## Checks and evidence

- `scripts/test.ps1`: 63 core tests, including real-surface laundry contacts, opt-in lab isolation, damaged support reaction, and tool snapshot/replay isolation.
- `scripts/laundry_check.ps1 -Rendered`: nine scene checks including three build-phase garments, wind amplitude, no presentation mutation of gameplay, render/query pose agreement, real glue use, no imaginary support, and physical F5 reset.
- `scripts/integration.ps1`: 25 engine checks.
- `scripts/access_check.ps1`: 16 movement/ladder/projection checks.
- `scripts/sculpt_check.ps1`: 11 sculpt/debris checks.
- `scripts/localization.ps1`: live switches, persistence, CJK glyphs and unchanged game facts.
- `scripts/smoke.ps1 -Players 1` and `scripts/smoke.ps1 -Players 4 -Port 18847`: accelerated three-round checks passed. Evidence: `artifacts/smoke-1-20260927-063047-443`, `artifacts/smoke-4-20260927-063057-635`; all three clients matched host completion hash `635862DC7AAF54D1`.
- `scripts/laundry_review.ps1`: GPU-rendered glue, growth and wind captures. The review driver sends actual tool input; wind capture begins with a short burst. It does not change tool damage or movement rules. `artifacts/laundry-glue.png`, `laundry-growth.png`, `laundry-wind.png`.

Final build/import: zero warnings/errors. The later F5 reset repair was recompiled and passed all nine rendered scene checks. Existing long-run sculpt acceptance is documented separately and predates this presentation revision; a new ten-minute run is not claimed here.

## Human acceptance still required

1. Without reading the commission, does the hanging laundry immediately explain the joke?
2. Glue the right support, then swap to the blower and use short bursts. Do movement and eventual breakage feel connected to your action, with enough warning?
3. Grow or clip one support. Does the customer look burdened while leaving the tool contact readable? Does the load wobble interfere with fine sculpting?
4. Walk around, raise/lower the chair and use Tab. Can you still judge the real 3D target and reach the work?
5. Submit, watch validation/replay, then F5 reset. Check that building the scene feels more like improvising a disaster than decorating a static gate.

Observed assessment: absurdity and customer reaction are clearer in rendered captures; motion and wind response are verified in-engine. Danger is tied to real tools, and the damaged/low attachment suggests near-failure. Whether this is funny and whether the tension is strong enough are human judgments, not automated pass claims. No missing executable or external asset blocker.
