# Visual Target Lab — 2026-10-01

**Current art route:** [Asset Search Round 1](ASSET_SEARCH_ROUND_1.md) supersedes the generated hero/furniture candidate below. Use that report for current imported assets, camera54° A, metrics,18 final captures and remaining art gaps. The rounds below are retained historical evidence, not current visual acceptance.

Owner redirected the next work from optional Phase8 lighting pipeline to an isolated visual slice. No new gameplay, states, shader features or full-level changes. The prior LookDev passes are mechanical evidence, not visual acceptance.

Reference hierarchy: supplied `01_human_concept.png` for human shape and hair identity; `02_human_gameplay.png` and `06_first_person_reference.png` for staging, foreground tool and teammate action. Purple alien foam is not a human hair material target.

## Scope and seams

- `Main.cs`: opt-in early launch; no session/network/audio is constructed.
- `VisualTargetLab.cs`: isolated room, fixed cameras, minimal UI and capture metrics.
- `TargetLabGeometry.cs`: rounded geometry and two local density-field presets, extracted by unchanged `HairShell`.
- `TargetLabActors.cs`: consistent rounded customer, chair, teammates and clipper.
- `scripts/visual_target_lab.ps1`: launch and six screenshots per run. No production preset or asset pipeline changes.

This is a posed visual slice, not playable multiplayer. B uses production FOV73, eye height1.7m, and records an actual density raycast against the existing3m clipper range. F1/F2/F3 cameras; F4 hairstyle; F5 gray/color; Tab first-person visual walk; Escape cursor; F12 screenshot. Objective/timer are fixture UI, not live round state. Ordinary `run_b.ps1` is unchanged.

## Round 1 planned priorities

1. Unified rounded character/chair/workbench shapes.
2. One side-part short cut and one swept tall pompadour in plain gray StandardMaterial3D.
3. Three fixed compositions showing full hair, face, teammates and foreground tool.

Then run six gray screenshots, compare with reference, record five biggest gaps and change only the biggest three in the next round. No shader additions.

## Round 1 review — artifacts/visual-target-r1-gray

Six real rendered frames and metrics retained. Biggest gaps, in order:
1. Giant hair is an undifferentiated elongated slab/banana, with no readable hair flow or cut plane.
2. Spot self-shadowing stripes and overexposed face/cape destroy the intended soft lighting.
3. A clips the giant crest; B tool obscures the right teammate; C leaves too much empty room.
4. Face mouth is buried, cheeks look attached; cape reads as two eggs instead of draped cloth.
5. Target card lacks a silhouette; static prompt implies an interaction the posed slice does not run.

Round2 changes only1–3: hair shape, lighting/shadow, camera/foreground framing. Round1 is not a gray-model pass. Build initially caught two C# declaration/overload errors, corrected before the actual run; failed log retained.

## Round 2 review — artifacts/visual-target-r2-gray

All six shots compared side by side with both references in comparison.jpg. Full silhouette is now framed and the acne/overexposure is corrected. Remaining biggest gaps:
1. Density sampling aliases the broad hair flutes into broken facets; still not a clean hair sculpture.
2. Buried mouth, stuck-on cheeks and two-piece cape make the customer expressionless and mannequin-like.
3. Scene is still muddy and flat; key/fill/rim separation is too weak.
4. Actors are symmetrically posed; raised tools do not convincingly meet the hair.
5. UI target silhouette and honest interaction status remain incomplete.

Round3 changes only1–3. Hair uses a lab-only2x coordinate scale in the unchanged lattice/extractor (effective7cm samples), transformed back on display AND ray queries. This is not a production density resolution change. Face and cape get continuous rounded shapes; lighting gets a brighter warm subject and cooler quiet background.

## Round 3 review — artifacts/visual-target-r3-gray

Six captures compared. Mouth/cape now read, but the round has an invalid cape boundary normal warning and is not accepted as clean. Biggest gaps:
1. Hair flow still has rippled, noodle-like normals and a dull rectangular crest.
2. Applied cheeks and a pill-shaped mouth still look like assembled parts; cape boundary normal needs repair.
3. Same-height symmetrical teammate gestures do not explain the haircut action.
4. Flat lighting and the target UI are still weaker than reference.
5. The short-hair shot leaves too much ceiling; fixed-camera consistency currently takes priority over per-style zooming.

Round4 only1–3: use the preset's continuous field gradient for clean hair normals, soften grooves and slope the clear cut; integrate cheeks/smile and repair finite cape normals; stage one barber on the existing workbench for the giant cut, with tool/hand endpoints at the shared hair. No extra props, gameplay or shader.

## Round 4 review — artifacts/visual-target-r4-gray

All six captures run and inspected; cape warning repaired. Biggest gaps:
1. Whole-head flat top still reads as a foam slab. Grooves have not solved the silhouette.
2. Barber clippers are buried in hair; right forearm crosses the face.
3. Target card has text only and the interaction prompt is misleading for a visual-only lab.
4. Warm/cool lighting remains less lively than reference.
5. Clothing and face remain simpler than target character art.

Round5 only1–3: replace the giant silhouette with an ascending curled crest surrounding a lower cut platform; pull hand/tool endpoints outside the surface; add a real 3D target portrait and remove the fake interaction prompt. Gray is still the decision view, no new shader.

## Round 5 review — artifacts/visual-target-r5-gray

Six gray views compared. The giant now has an identifiable swept crest, negative space and lower cut platform instead of a box; full hair/face/one unobscured teammate/foreground tool fit A/B, with tool action readable at thumbnail scale. B ray distances: ordinary2.388m, giant2.324m at production73°/1.7m. This is a provisional gray-shape milestone, not owner approval.

Biggest gaps:
1. Lighting is still flat and the palette looks gray/olive rather than warm toy advertising.
2. Surface values/roughness are too similar; the foreground hair lacks material identity in color.
3. Eye proportions/gaze and garment shading still read as a mannequin; cape normals were oriented inward.
4. Short-style fixed-camera framing leaves more ceiling than ideal.
5. Sculpted hair flow is broad/simplified versus the reference's authored S-shaped locks.

Round6 only1–3: stronger warm key/cool fill, quieter teal background and chestnut hair, varied existing StandardMaterial3D roughness, integrated eyes/gaze and outward cape normals. Still no hair shader or texture feature. Run both gray and color A/B/C to keep the shape check reviewable.

## Round 6 review — artifacts/visual-target-r6-{gray,color}

Twelve actual frames compared. Toy geometry, teal/cream/wood palette, visible face and action/target UI are substantially closer in their relationships. Remaining biggest gaps:
1. Orange hair is oversaturated and too uniformly lit; needs a quieter chestnut value.
2. Analytic normals on sampled triangles produce small root/flow shading discontinuities.
3. Key cone/shadow transitions are too hard for the soft toy-ad target.
4. Faces/clothing are still simple generated art, below reference character quality.
5. Fixed short-cut composition retains excessive headroom, while giant composition is stronger.

Round7 only1–3: darker chestnut standard material; area-weighted shared-vertex normals on the same triangle positions; wider, softer key cone and lower shadow opacity. Then freeze candidate, recapture gray/color, validate hotkeys and unchanged gameplay sources, and present the remaining art gap for owner acceptance. No claim of pixel match or production rollout.

## Round 7 review — artifacts/visual-target-r7-{gray,color}

Rejected the mesh-normal experiment after actual screenshots. Biggest gaps:
1. Area-weighted normals expose tetrahedral facets and regress toward rock; revert them.
2. Workbench teammate face is partly covered by the crest/arm.
3. Cape remains too blank and stiff compared with a draped garment.
4. Short-cut camera headroom remains generous.
5. Generated faces/furniture remain simpler than authored reference assets.

Round8 only1–3: restore continuous-field normals with a modest derivative footprint, move the bench teammate20cm outward, deepen existing cape geometry folds. Retain the darker chestnut and softer lighting from7. No added shader features. Final captures and tests must use this frozen build; round7 is retained as a failed visual direction.

## Round 8 / owner review candidate

Final evidence: `artifacts/visual-target-r8-gray/` and `artifacts/visual-target-r8-color/`. Each contains ordinary/giant × A/B/C PNGs,384px thumbnails, reference comparison.jpg, metrics.json and candidate.json. Across eight rounds there are66 full runtime screenshots, plus thumbnails. Candidate DLL SHA256: `ACDD59CCAA2C879675AEA3C5E8E41127B3716E628D1EF5D9CDFB63B141BEC355`.

Observed: full giant crest and face fit A/B/C; foreground clipper and at least one fully visible teammate fit A/B; the two levels of teammate staging explain the giant haircut in C without UI. Gray and color share exact geometry. Giant platform/crest and ordinary side part are distinct. Rounded eyes/nose/body/chair/bench/tool belong to one simplified toy family. UI has only objective, fixture timer, real target portrait, tool and reticle. No tutorial/debug paragraphs. These are visual improvements, not proof of authored-asset quality or owner approval.

Remaining five gaps, ranked:
1. Hair lock organization is still broad, soft and simplified; root edge/flow lacks the reference's carefully authored interlocking S-curves.
2. Expressions and anatomical transitions are simpler and stiffer than the reference character art.
3. Short-hair A/C composition has generous headroom because camera poses are fixed across both presets.
4. Cloth, furniture and room lack the reference's subtle surface variation and more natural lighting gradients.
5. This is a posed composition: animation, real multiplayer crowding and hands performing actual cuts have not been accepted in this visual layout.

If owner requests another visual round, prioritize1–3; do not add hair states, VFX, gameplay or migrate the full level to cover these gaps. Current stop is owner visual review, not rollout to B or completion of the earlier full HairVisual pack.

### Actual validation

- Build/import passed, two pre-existing nullable warnings in GesturePropsView/PartyMoldView.
- Core248/248 (`artifacts/visual-target-core.log`), no Core source changes afterward.94 Core files hash-identical to entry baseline. Main identical except the opt-in early Lab launch line (`visual-target-{core,main}-invariance.json`).
- Actual native F1/F2/F3/F4/F5/Tab/W/Escape/F12 dispatch passed, finite mesh/normals, exact mesh/color rollback and no production authority scene (`visual-target-controls.log`). Manual free-walk captures use `free`, never the fixed B filename.
- Existing rendered shared SculptChecks passed using `scripts/sculpt_check.ps1 -Rendered` (`visual-target-shared-sculpt.log`). An earlier extra `--v06-variant-b` invocation failed its vacuum fixture assertion and remains in `visual-target-mainline-sculpt.log`; that is not counted as a B pass. No gameplay code changed to accommodate this old low-level fixture.
- Rendered actual B WorldChecks passed cat/rescue/drag/native slap/three-round photos/history/bilingual checks (`visual-target-b-world.log`, EXPANSION_13_ENGINE_OK). No new network/WAN acceptance claimed; protocol and Core unchanged.
- Final1280×800 Compatibility/RTX5070Ti Laptop, per-shot2sec measurement after1sec warmup, no simulation: all gray/color frames0.716–0.974ms average, GPU0.250–0.291ms,202–219 draws. Hair triangles: ordinary8100, giant19048. This is an isolated static render sample, not a full-game frame budget or multiplayer performance result.
- B remains73° FOV and1.7m eye height; ordinary center-ray hair hit2.388m, giant2.324m, both inside current3m clipper range. This verifies camera geometry, not live tool gameplay.

### Commands and rollback

```powershell
pwsh -NoProfile -File scripts/build.ps1
pwsh -NoProfile -File scripts/visual_target_lab.ps1 -Interactive -Color
# Omit -Color to start with gray hair; omit -Interactive for automatic six-shot capture.
pwsh -NoProfile -File scripts/test.ps1
pwsh -NoProfile -File scripts/sculpt_check.ps1 -Rendered
```

F1 A / F2 B / F3 C; F4 ordinary/giant; F5 gray/chestnut; Tab grounded visual walk, WASD + mouse; Escape cursor; F12 save image. This lab does not run cutting/round logic. Simply use `scripts/run_b.ps1` to return to the existing complete game. No renderer, settings, production art or core asset pipeline was changed. To remove the lab source, first remove its single opt-in entry in Main, then the three new TargetLab/VisualTargetLab C# files; baseline Main is preserved in `artifacts/visual-target-baseline/Main.cs.before`.
