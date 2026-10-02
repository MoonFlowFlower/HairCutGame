# Hair Visual LookDev — 2026-09-30

Source: owner request + HairVisual Implementation Pack v0.1 (README, docs 00–08, all six reference images read). All eleven existing project reference images inspected. Pack instructions are technical proposals within the owner's request, not permission to change unrelated gameplay.

## Audit / smallest seams

- Authority: HairVolume (.14 m lattice) → HairShell marching tetrahedra. SurfaceRaycast uses those same triangles. Sculpt/GrowAlong, material patches, Validation/Scoring/StyleAttributes and wire/replay remain unchanged.
- HeadView currently expands indices to triangle vertices with flat normals; RGB holds already mixed material color, UV holds mobility/roughness. Old shader has perpetual sine motion. The presentation seam can smooth normals and add material state without replacing extraction or moving any vertex in the authoritative field.
- Art.Shop is a code-generated room, no ceiling, one shadowed directional light, ambient color + Linear tone map. Actual engine: 4.7.2 stable Mono, gl_compatibility, RTX5070Ti laptop. Official 4.7 docs confirm simplified SSAO (radius/intensity), shader instance parameters and Compatibility restrictions.
- Debris already has authoritative capped flights/piles, reusable MultiMesh batches, conserved vacuum/blower interactions and capped detached fragments. Reuse these facts; cosmetic objects must never create gameplay mass or new collisions.
- No Git checkout present. Recovery: artifacts/hairvisual-before.zip SHA256 1E2F4F8661B544FFFF26F1DA859B6D86C6812048912297F36F7061877301E438. Restore separately, never overwrite newer changes blindly.

## Conflicts / handling

- Pack F-c4 reference is older than source AI wake/menu fixes: baseline is current source, not the old package.
- Pack asks four customers/workstations: project locks ONE shared customer. Stress uses four visible barbers, one maximum shared head, debris, wind and concurrent tool feedback. No extra customers introduced.
- Authoritative states are Patch wet/temperature/char/glue, not the starter's proposed gameplay struct. Visual encoding reads these existing values only.
- Official engine docs are checked against real installed API and rendered execution. No Forward+ migration or unsupported effects.
- Ceiling is visual only: adding a collider would change existing tool/world queries. Shop walls/physics stay intact.

## Workflow / rollback

`pwsh -NoProfile -File scripts/build.ps1`

`pwsh -NoProfile -File scripts/visual_lookdev.ps1 -Phase 0 -Profile Current`

Explicit opt-in only; ordinary run_b keeps old visuals. F9 in LookDev cycles Current/Human/Animal/Alien; F10 toggles scene layer. Quality flags: -Disable flock,rim,micro,dynamics,debris,ssao,glow. Anisotropy stays off (no reliable tangent pipeline).

Measurements use 1280×800, fixed camera, four visible barber fixtures, production SalonView/HeadView and HUD. Two-second warmup, monotonic frame intervals, CPU render setup, actual viewport GPU timer, draw calls/objects, triangle counts, production presentation sync time. `onePercentLow` means reciprocal P99 frame interval. QA world does not simulate; pre/post full WorldState wire SHA256 must match. Cross-run world hashes differ due to match GUID; invariance is checked within each run. This frozen-scene timing is not full multiplayer CPU evidence.

## Phase evidence (append after actual runs)

- P0: build/import pass; artifacts/lookdev-p0-baseline screenshot + thumbnail + metrics. 386.04 FPS, 2.590 ms/frame, P99 reciprocal255.19FPS, GPU0.356ms,794 draws,30124 hair triangles, authority unchanged. Earlier p0-current has mispositioned barber-hair fixture; excluded from comparisons. Corrected head/face poses match actor positions.
- P1 initial: build/import pass; artifacts/lookdev-p1-current. Ceiling/warm key/quiet environment/blob/SSAO active; unchanged legacy hair shader.390.85FPS,2.559ms/frame,GPU0.491ms,832draws,30124triangles,authority unchanged. Screenshot inspection found ceiling beam shadows crossing hair; candidate corrected to non-shadowing overhead decoration and reduced key energy. Final P1 evidence to follow.

Human visual acceptance, real WAN and subjective comfort remain open. Mechanical pass does not prove hair material identity or toy-ad quality.

- Frozen fixture introduced after P1: the same serialized fixture now gives identical normal-state world hash 77876B1A... across phases. P0 fixed: 413.56FPS/2.418ms/GPU0.298ms/783draws. P1 fixed:365.71FPS/2.734ms/GPU0.531ms/796draws. Earlier independently randomized fixture timings are descriptive only.
- P2 final: artifacts/lookdev-p2-final-{normal,flat,hole,wet,frost,burn,overgrown}; actual Compatibility screenshots inspected, authority unchanged in all seven. Normal402.51FPS/2.484ms/GPU0.564ms/795draws/30124triangles. State range368.9–421.8FPS. Five actual palette runs (natural, black, blond, red, dyed) inspected. Final shader reduces flow contrast; tolerance-ring shadow removed only in opt-in scene; overgrowth fixture retains production face clearance. Existing geometry, wire and scoring unchanged. Build/import pass; Core248/248. No claim of owner visual acceptance.

- P3: animal uses the same opaque pass with fleece-cluster modulation, roughness .94 and restrained stronger rim; no transparent overlay. Normal/flat/wet screenshots inspected (artifacts/lookdev-p3-*), authority unchanged. Flat silhouette retains exact cut plane; wet suppresses flock variation. Metrics in lookdev-p3-summary.json. This is a material preview on the existing human fixture; production Wool selects this material automatically, no new species or gameplay added. Plush identity still needs owner comparison.

- P4: saturated purple soft-vinyl/foam preview, restrained backlight and micro detail, still opaque single-pass. Normal/flat/burn actual screenshots inspected (artifacts/lookdev-p4-*); dry-char state stays matte/dark, no organic drops or strings. All authority hashes unchanged; metrics lookdev-p4-summary.json. Alien is a local material preview only (the game has no alien customer enum).

- P5: vertex COLOR R/G/B/A = wet/frost/burn/glue; trilinear visual interpolation of cached nearest existing patch samples. Smooth normal cache avoids repeated gradients. Human/Animal/Alien mixed-state screenshots inspected; all three use identical authority hash6897A16B... and unchanged30124triangles/795draws. Human411.82FPS/GPU0.523ms, Animal417.45/GPU0.525ms. Animal normal relief refined without displacement, cut planes suppress large relief. Material count stays one shared shader. No scoring/contact state interpolation is introduced.

- P6: event-only damped visual spring, head-motion acceleration/rotation and actual tool-contact/blower coupling; persistent resting props do not re-trigger. Wet adds mass/damping, frost/glue add stiffness and suppress compliance. Hard world displacement limit2.5cm, root mobility mask; no authority vertex/collider moves. Large-impulse normal/wet runs both hit the cap (retained, not state contrast proof); smaller repeated runs below cap in lookdev-p6-small-* include actual rendered screenshots/event.png, per-frame spring.json and metrics. Rest=0 and final=0 for all; peaks/overshoot in lookdev-p6-small-summary.json. All authority unchanged. Earlier QA compile typo HairState.Soft repaired to declared Normal; failed build log retained, no stale assembly used.

- P7: reuses authority DebrisSystem flights/deposits/replay/vacuum/blow; adds32 pooled matte fiber-textured key clumps,256 existing airborne instances, at most4096 enhanced resting chips (legacy max16384 retained when Current). No RigidBody duplication, collider or mass added. 512-pile/64-flight frozen scene screenshot inspected; first overly round smooth clumps rejected and replaced by flattened flocked clumps. Ground camera evidence lookdev-p7-floor-final; ordinary view lookdev-p7-debris. Live debris history remains authority-owned and bounded. Deferred clothing attachment is optional. Contextual static prop labels now suppressed after presentation sync; reaction/state cues remain system-driven.

- Final integration checks: Core248/248 and rendered SculptChecks pass (lookdev-final-core.log / lookdev-final-sculpt.log). First4P run completed gameplay but failed on a cached contextual label removed from the scene before QueueFree (lookdev-final-four.log retained). FinishHints now requires IsInsideTree and not queued before GlobalPosition; repeat pending. Reference-goal previews retain legacy zone colors; visual switches cannot recolor required reference markings. Scene has an explicit no-scene switch as well as F10 in LookDev.

## Current frozen source / launch / acceptance boundary

- Source candidate: phases0–7 only. Build/import lookdev-final-cache-build.log passes with the two existing nullable warnings. Core248/248 unchanged. Frozen Debug assembly hash in artifacts/lookdev-final-cache-assembly.sha256; check it before/after final native/network runs.
- Uniform caching removes redundant per-frame material submissions; zero springs sleep, Current skips candidate spring work. Exact same normal-world wire SHA77876B1A... and30124hair triangles before/after. Final sequential 1280x800 frozen-scene captures: artifacts/lookdev-final-cache-baseline/frame.png vs lookdev-final-cache-human/frame.png; thumbnails inspected. Baseline419.45FPS/2.384ms/P99 reciprocal293.96/GPU0.336ms/783draws; candidate424.16FPS/2.358ms/P99 reciprocal282.77/GPU0.468ms/763draws. Difference in mean FPS is short-sample noise, not a proven speedup. GPU cost +0.132ms. CPU presentation sync0.672/0.675ms. Pre-cache slower candidate evidence retained separately.
- Final mixed local-state screenshot: lookdev-final-cache-mixed, vertex-state ranges0→1 across all four channels; no material multiplication. Final impulse lookdev-final-cache-impulse: peak18.11mm and settles0, unchanged authority. Earlier four-state smaller-impulse traces remain evidence of wet/frost/glue response.
- Source audit:161 original Core/Bootstrap files unchanged, excluding Main.cs. Main.cs separately compared to recovery ZIP after removing exactly the local config / LookDev-entry lines: identical. See artifacts/lookdev-authority-source-invariants-final.json and lookdev-main-source-invariant.json. No project renderer settings changed.
- First native WorldChecks command omitted --v06-variant-b and ran historical Off: cat-weight assertion failed. Retained lookdev-final-world.log; no gameplay repair. Correct B command passes in lookdev-final-world-b.log (cat/E rescue, bounded drag/opt-out, actual LMB wake, three rounds/photo history, bilingual).
- Completed pre-cache-fix 4P rendered-host candidate +3 legacy headless clients:40.617s, all exits0, final hash agreement (lookdev-final-four-fixed.log). A final repeat on the cached candidate is recorded below once finished; do not combine assembly versions as one suite.

Playable candidate:

    pwsh -NoProfile -File scripts/build.ps1
    pwsh -NoProfile -File scripts/run_visual_b.ps1
    pwsh -NoProfile -File scripts/run_visual_b.ps1 -Mode Host -Port 7777
    pwsh -NoProfile -File scripts/run_visual_b.ps1 -Mode Join -Address 127.0.0.1 -Port 7777

Normal legacy mainline remains `pwsh -NoProfile -File scripts/run_b.ps1`. This task did not replace a delivered ZIP. Candidate -Profile Human selects Animal automatically for actual Wool customers. -Profile Animal/Alien are explicit local material previews; no new customer types. Full rollback: -Phase0 -Profile Current or ordinary run_b. Scene-only off: -Disable scene. LookDev F9 cycles profiles/F10 toggles scene; interactive mode is a frozen material fixture, not the playable game.

    pwsh -NoProfile -File scripts/visual_lookdev.ps1 -Phase 7 -Profile Human -Interactive
    pwsh -NoProfile -File scripts/visual_lookdev.ps1 -Phase 7 -Profile Animal -Llama
    pwsh -NoProfile -File scripts/visual_lookdev.ps1 -Phase 7 -Profile Human -State mixed
    pwsh -NoProfile -File scripts/visual_lookdev.ps1 -Phase 7 -Profile Human -State debris -FloorCamera

Phase8 is pending the owner's asset-pipeline choice in docs/HAIR_VISUAL_PHASE8_DECISION.md. No LightmapGI bake, reflection probe, glow/tone polish, or Phase9 full maximum-head/wind/debris combined stress and per-switch cost table has been claimed. The glow flag is reserved for Phase8 and currently has no effect. All0–7 active effects can be disabled or reverted; formal defaults remain legacy.

Owner visual acceptance stays open: human hair identity at360px; animal plushness vs cream/foam (still the weakest profile); alien toy identity; wet/frozen/char/glue readability; spring amplitude and hit alignment; floor clump identity. No claim of matching conceptual toy-ad quality. Real Canada–China route and subjective comfort remain previous product acceptance gaps; no network-motion changes were made.

Official references used during the audit: https://docs.godotengine.org/en/4.7/tutorials/rendering/renderers.html ; https://docs.godotengine.org/en/4.7/tutorials/3d/environment_and_post_processing.html ; https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html . Runtime installed API/render tests are the final evidence for this candidate.

- FINAL cached assembly: 55DECD2FEF60E258D73C4A7B818402D2158DD4C6C2A4FA5ACB3112FAE95D8BFB. Frozen4P rendered-host candidate +3 legacy clients, three-round rendered B WorldChecks and rendered SculptChecks all pass, all native exits0, clean error scan. Logs artifacts/lookdev-final-cache-{four,world,sculpt}.log;4P final hashes agree. The same DLL hash stayed unchanged through final captures/impulse/mixed/native/network checks. Core248/248 source unchanged. Earlier failed/previous-assembly runs remain distinct. Phase8 pipeline decision is still pending; Phase9 is not claimed.

- Final safety verification used an artifact-only GDScript harness (no product assembly rebuild) to dispatch actual F9/F10 events through the production LookDev node: four profiles selected, mesh vertex arrays identical, Current restored legacy shader, scene off/on worked. Log lookdev-final-toggle.log contains VISUAL_TOGGLE_CHECK_OK and unchanged authority. Its performance samples span toggles and are excluded from the baseline/candidate timing comparison.
