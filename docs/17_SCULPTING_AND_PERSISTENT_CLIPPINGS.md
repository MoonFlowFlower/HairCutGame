# Controllable sculpting and persistent clippings — implemented revision

This user-approved playtest revision supersedes the older basic-tool brush and disposable clipping behavior. It keeps manual first-person editing, the unified faceted shell and the existing density resolution. It adds neither automatic target matching nor a cleaning score.

## Input and intent

- Clipper and spray: left mouse normal, right mouse fine, wheel small/medium/large. Other tools retain their secondary action.
- Clipper radii: 0.14/0.21/0.28 m; normal/fine depths: 0.05/0.015 m. One press locks a local plane. Dragging covers that plane; holding still cannot deepen it. Release/repress removes another layer.
- Spray radii: 0.21/0.28/0.42 m; normal/fine rates: 0.18/0.045 m/s. The gesture keeps its initial outward direction, with smooth radial falloff and interpolated dabs. A floating-point working field avoids rounding away small time steps. Ordinary spray retains collateral targets and airflow deflection.
- Facial geometry scales radius and displacement. Scalp lattice resolution remains 0.14 m; independent details below that scale are not promised.
- The live footprint shows contact, range and outward direction. Fine mode is gold; invalid contact is red. Preview uses the host's tetrahedral surface and occlusion query. Clients predict presentation; snapshots decide the actual edits.
- Changing tool/mode/radius resets the context. Releasing, submitting or losing the original growth target ends that stroke. Active local-target shell rebuilds are prioritized at approximately 20 Hz.

## Geometry and material

`ToolQuery` produces `BrushContext`; the host's `SculptStroke` owns the plane/direction. Surface rays intersect the same tetrahedra as the rendered shell. Connectivity uses those tetrahedral edges, including diagonals. All rooted or effectively anchored components survive; glue alone is not support. A loose object's main component remains on that object while additional detached components split away.

`VolumeEditResult` separates removal, direct absorption, visible removal and detached volumes. Vacuum absorption is counted once. Other removed material falls from the actual hit with the source color. Large severed components retain their density shape and state. Their floor contact uses the lowest visible shell point, including rotation, rather than the invisible density-band bounds.

## Falling and accumulation

- At most 64 authoritative flying batches drive 256 animated chip instances. Batches hold mass/color/velocity; individual chips have no node, body or network transform.
- Swept support queries catch floor, benches, visible chair surfaces and ladder treads. Resting material aggregates in approximately 0.4 m surface regions. Moving/resetting a support releases its clippings again.
- Piles use one MultiMesh, at most 2,048 regions with eight visual pieces each. Scatter, coverage and capped height express increasing material. At the cap, material merges into nearby entries rather than disappearing.
- At most 12 unattached complete fragments remain interactive. Fragments resting without tool use for 15 seconds convert into piles. Official wigs, attached and anchored objects are excluded. Additional severed material over budget uses falling batches.
- Piles do not collide with players or become climbable terrain. Fine clippings have no individual burn simulation. Complete fragments retain burn/freeze/glue interactions.
- Vacuum collects piles into its reservoir. Capacity stops collection with a Chinese/English notice; right mouse transfers stored hair. Blower lifts and redistributes existing material without copying it.
- Clippings persist across all three rounds. New match / laboratory F5 clears them. No compulsory cleaning or hygiene score.

## Multiplayer and replay

The host owns emissions, aggregate trajectories, landing, merging, cleaning and reservoir material. Compressed snapshots include flight batches, piles and complete fragments. Late join receives the accumulated scene. Clients add cosmetic rotation/spread; snapshots remain authoritative.

Replay frames clone historical debris state and draw it directly. Playback never emits into the live scene or duplicates live material.

## Verification

From the repository root in PowerShell:

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/integration.ps1
./scripts/sculpt_check.ps1
./scripts/sculpt_check.ps1 -Rendered
./scripts/access_check.ps1 -Rendered
./scripts/smoke.ps1 -Players 1
./scripts/smoke.ps1 -Players 4 -Access
./scripts/sculpt_stress.ps1 -Duration 600 -RenderedHost
./scripts/sculpt_review.ps1
```

The stress fixture alternates clipper/spray through real local/client input, renews finite customer hair every 30 seconds to sustain edits, and joins peer four after clippings exist. It measures 600 seconds after all four join, logs counts/memory/processing time, verifies late-join material and compares final snapshots. This is a localhost load test, not a real-latency quality claim.

Actual evidence belongs in `PROJECT_STATE.md`. Human acceptance remains: deliberate shaping, useful fine/normal modes, and a convincing sense of cutting material as the floor becomes messy.

Completed stress directories also contain `metrics.json`: observed budget maxima and separate warm/late memory and processing-time windows. Regenerate this report with `./scripts/summarize_sculpt_stress.ps1 -Directory artifacts/<completed-run>`. Processing time is the script/physics work measured per frame, not a full rendered-frame/FPS guarantee.
