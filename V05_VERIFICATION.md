# v0.5 Material Grammar — verification and playtest handoff

Incremental on the existing v0.4 project. No restart, external art dependency, Inspector wiring, or new service. Backup: `artifacts/v04-before-material-20260927-153303.zip`.

## Implemented
- Same first-person 1–4-player shared head, 75-second construction, shared wallet, customer reactions, physical interference and post-job ego awards.
- Tab-held offhand board: actual front/side/top orthographic renders from the scoring primitives, with example prop placement. Shared protected maquette. H-held ghost is optional. Reference artifacts cannot enter gameplay hair/material accounting.
- Six material states on the existing coarse controls: Normal, Young (matures), Wet (dries/resists fire/weakens fresh glue), Frozen (rigid/brittle/thaws), Glued (durable fixation/heat release), Charred (persistent, brittle/irregular erosion). Water mister supplied. A six-sample interactive material lab is available; samples evolve naturally and F5 resets them.
- Continuous faceted outer mesh retained. Bounded local shader motion and state-dependent surface roughness/contact sounds; no soft-body or strand solver.
- Clipper locks a shallow finishing plane; growth extrudes Young material; blower pushes or locally presses/organizes; vacuum pulls/collects/transfers and carries material metadata; trimmer cuts broad horizontal or aim-facing planes; glue bonds touching loose shells in place; sniper punctures; flame heats/chars/erodes. Effects use a common query for previews and authority.
- All eight existing goal types receive construction-time props. E carries/places, R/F tilts, G drops. Simplified gravity queries hair, scalp, furniture and floor. Unsupported props fall. Validators keep the same IDs and real starting positions. The staged helicopter itself approaches and lands. Missing props are not fabricated; an uninserted rocket cannot succeed from above the silo.
- Nest target includes a real bottom. Birthday Cake requests a charred outer zone and real placed candles. Goal-specific Shape + Material + Function weighting, failure caps and corresponding team payout. Live measured readiness contributes to the existing impact ledger. Props/materials/reference poses are recorded in snapshots and replay.

## Verification actually executed
- v0.4 baseline before changes: build + 94/94 core tests.
- Delivery build: Godot 4.7.2 Mono, 0 warnings / 0 errors (`artifacts/v05-delivery-build.log`).
- Delivery core: 109/109 passed (`artifacts/v05-delivery-tests.log`). Includes material transitions, transport tolerance, board/tool isolation, reference geometry, missing/unplaced props, insertion gate, falling support, contact bonding, snapshot/replay immutability and live readiness cache invalidation.
- Four uneven-pad correction routes exercise the actual session tool pipeline within legal FPS pitch limits. Quality starts at 70; clipper 89.8, vacuum relocation 88.4, blower press 89.9, trimmer plane 99.2. All become physically viable after glue stabilization; relocation checks conservation. These are repeatable fixtures, not a claim of human usability (`artifacts/v05-routes-corrected.log`, latest delivery tests).
- Real engine integration passed, including three render viewports, visible board raise/lower, reference immutability and construction/validation prop identity (`artifacts/v05-delivery-integration.log`).
- Entire existing regression runner passed: shared, access, sculpt, localization, laundry, ego, normal shop walkthrough and host-loss recovery (`artifacts/v05-release-regression.log` and `artifacts/v05-wrapper-verification.log`).
- Full solo match: three actual 75-second jobs passed (`artifacts/v05-solo-full.log`).
- Full two-player match: three 75-second jobs, shared orders/awards and snapshot agreement passed (`artifacts/v05-release-2p-full.log`).
- Four-player shared orders/awards and final state agreement passed (`artifacts/v05-release-4p.log`).
- Delivery four-player disconnect + ego combined smoke passed (`artifacts/v05-delivery-4p-disconnect.log`): remaining peers completed all three jobs and agreed on snapshots/awards. The launcher now correctly excludes the deliberately departed peer from subsequent award-message assertions; the corrected harness was rerun successfully.
- Ten-minute, four-peer sculpt load with rendered host and late join passed (`artifacts/v05-stress.log`). Late peer received 1.691 units of existing debris. Final hash `956052A798BF38DC` agreed across peers. Material/prop late snapshot and immutable replay are additionally checked by core tests. Headless smoke peers skip cosmetic remeshing; engine integration and rendered-host tests retain it.
- Actual rendered screenshots reviewed: `artifacts/v05-final-board.png`, `artifacts/v05-final-materials.png`; no shader/engine errors in their stderr logs.

## Load evidence and limits
Host: debris 209.606 units; maximum 31 piles, 11 flight batches, 3 detached chunks, 247 visible clipping instances. Ledger bounded at 2048 events. Working set 530–747 MB; warm-window mean 677.23 MB, late 720.69 MB. Script/physics means 3.65 ms warm and 6.60 ms late. These are not full GPU render frame timings; local concurrent verification affects timing. See the saved metrics JSON for all peers.

## Honest acceptance boundary
No executable/dependency blocker remains. Human experience acceptance is still open. Material zones remain 32 coarse regions on a 0.14 m lattice; very small patterns cannot be promised. Transport conservation is to lattice quantization tolerance, with correction localized to edited surface samples. Cosmetic motion does not drive collision. Props use simplified support/gravity and validator motors (including scripted cat/car/rocket motion), not full rigid-body, cloth or fluid simulation. The six material visuals and tool feel need playtesting, not a stronger automated claim.

Highest-value playtest: hide H ghost and follow board/maquette; compare blower press, vacuum relocation and plane cutting on an uneven pad; distinguish the six materials; place three eggs and cut away their support; glue and heat-release a touching offcut; judge whether shared prop preparation creates comedy rather than busywork.

## Commands (PowerShell, repository root)
```powershell
./scripts/build.ps1
./scripts/run_solo.ps1 -Lab
./scripts/run_solo.ps1 -Materials
./scripts/run_solo.ps1
./scripts/run_host.ps1 -Port 7777
./scripts/run_client.ps1 -Address 127.0.0.1 -Port 7777
./scripts/run_4p_local.ps1
./scripts/verify_v05.ps1
./scripts/verify_v05.ps1 -Network -FullDuration -Stress
```
