# v0.6 controlled A/B experiment

> Historical experiment report. On 2026-09-29 the owner selected B as the development baseline and archived A. Original results/commands below describe that earlier revision, not current defaults. See [current baseline](docs/42_B_DEVELOPMENT_BASELINE.md).


This is an additive experiment in the current repository. The unflagged v0.5 + interactive-miniature game remains available. Source backup: `artifacts/v05-before-v06.zip`. No Git repository was present.

## What is playable

Both explicit variants use the same initial human, helipad commission, seed 729, 75-second work budget, salon, controls, tools, hair representation and multiplayer authority. An explicit variant runs **one commission**, then allows Enter to repeat. This lets the observer follow A–B–B–A–A–B without extra missions or customers. Ordinary launch retains the existing three-job game.

- **A:** current helipad baseline. Offhand technical board, optional H ghost, interactive practice miniature, unlimited basic growth, existing panic reactions, final-15-second rotor approach, locked work/spectator validation, original scoring and results.
- **B:** outcome poster and functional landing. No exact miniature, technical board/ghost or visible shape score. The original helicopter stages in view, passes at 45 seconds remaining, approaches at 20, descends at 10, contacts the actual live hair, and needs 3 uninterrupted seconds of support. Work stays live through contact and stabilization. A failed attempt resolves by six seconds after the construction countdown expires. There is no early-call bonus.
- B customer attention: directional game-world sound onsets, mirror-visible danger, helicopter/gear attention, a 1.15-second `?` notice, 0.4-second `!` commitment and a two-second physical response. Actual hair/tool queries, collision proxy and presentation share the head transform. Repeating a source class/direction is habituated until eight quiet seconds; sustained motor input creates one onset. Sniper zoom is silent.
- A player's body or the carryable magazine can physically intersect the customer-to-mirror proxy route. Blocking the route or removing visible danger during notice cancels that visual reaction. The new mirror shows the customer; the existing personal portrait remains separate. No optical reflection simulation or microphone access.
- Any player can hold E while near/aiming at the head to brace. Reaction movement falls to 22%, never zero. Primary/secondary actions and passive blower/trimmer effects stop while bracing; the held slot is retained. Release, input timeout, departure, result and reset clear the brace.
- Downwash changes soft hair, loose hair/debris and light objects through existing forces. Existing blower/suction prop impulses also compete with the helicopter motor. Support is sampled under the world-space landing footprint, including attached hair; head tilt, low resistance, flame, lost contact and customer recovery can interrupt dwell. Accumulated customer damage of 50 is a commission failure. This is a lightweight deterministic support/motor model, not rigidbody aerodynamics.
- Shared B growth reservoir: 3 material units, about 15% of the initial 20.34 units. All growth copies and collateral growth draw from it; swapping/dropping tools does not refill it. A final dab exceeding the remainder is rejected and exhausts the small remainder. Paid growth bursts are disabled and visibly marked supplied; other existing tools remain. Recycling existing hair is still legal. Outcome and payment do not use shape; ordinary accident deductions remain.

## Launch from repository root (PowerShell)

```powershell
./scripts/build.ps1
./scripts/run_ab.ps1 -Variant A
./scripts/run_ab.ps1 -Variant B

# LAN host; press Enter after everyone joins.
./scripts/run_ab.ps1 -Variant B -Mode Host -Port 7777
# Each other computer, using the host's LAN address:
./scripts/run_ab.ps1 -Variant B -Mode Join -Address 192.168.1.20 -Port 7777
```

Use A instead of B for the baseline. The host's variant is authoritative. `GODOT_BIN` overrides the locally discovered Godot 4.7.2 Mono executable. These scripts invoke the existing built project; run the build first after copying a source revision. The equivalent engine flags are `--v06-variant-a` and `--v06-variant-b`; specifying both is rejected. Existing `run_solo.ps1`, `run_host.ps1` and `run_client.ps1` retain ordinary behavior.

## Reproducible checks

```powershell
./scripts/verify_v06.ps1 -Network -Rendered
./scripts/v06_solo_calibration.ps1 -Runs 3 -Rendered
./scripts/smoke.ps1 -Players 4 -Variant B
./scripts/smoke.ps1 -Players 2 -Variant B -FullDuration
./scripts/smoke.ps1 -Players 4 -Variant B -DisconnectClient
./scripts/host_loss.ps1 -Variant B
```

`v06_solo_calibration.ps1` uses ordinary movement, E rack pickup, existing ladder collision, blower secondary and glue primary inputs. It never assigns hair geometry, material state, player transforms, ownership or extra growth. It is a deterministic feasibility fixture with access to support measurements, not evidence of human skill, enjoyment or waiting time. The separate `v06_review.ps1` deliberately stages presentation fixtures; its images are **not** proof that a player constructed their displayed pad.

## Evidence actually run

- Before production changes: build/import, 114 core tests, engine integration and four-player ordinary baseline smoke passed. Logs: `artifacts/v06-baseline-{build,tests,integration,4p}.log`. Baseline final peer hash: `A8A82736155DFF39`.
- Updated pure suite: 128 tests passed in `artifacts/v06-tests-delivery.log`. New coverage includes A invariance, real downwash, same helicopter, live editing, mirror blockers/cancellation, stage timings/direction, motor habituation, silent zoom, brace lifecycle, finite shared supply, world-space support/dwell failure, helicopter impulses and immutable snapshot/replay data.
- Six full logical solo recipes (three repetitions each of clipper+glue and blower+glue) start from ordinary untouched hair and succeed without growth. After the landing-gear attention cue, resolution is approximately 0.6–1.0 seconds after the nominal countdown, within the live stabilization allowance. These recipes use legal aim limits and existing tool queries; positions are a logic fixture.
- **Delivery runner:** `./scripts/verify_v06.ps1 -Network -Rendered` passed. Complete evidence: `artifacts/verify-v06-20260927-183449/`; top-level log `artifacts/v06-delivery-verification.log`. Build/import: zero warnings/errors. Existing engine suites: 102 assertions (general 43, shared 8, access 16, sculpt 11, laundry 9, ego 15); new v0.6 engine suite: 17 assertions. Localization, ordinary rack walkthrough and ordinary host-loss recovery also passed. The runner's pure-test log confirms 128/128.
- A single-player and four-player smoke passed. B single-player, **two-player full 75-second construction timeline**, four-player and four-player/client-departure smoke passed. All surviving peers received the authoritative intermediate flight and notice/commit/reaction facts; multiplayer B additionally checks remote bracing. Final snapshots matched the host. B host loss returned the client to the menu. These smoke tests verify completion and synchronization; their unskilled bots are allowed to fail the commission.
- **Three rendered solo feasibility runs passed**, with original rack pickups, physical staircase traversal, ordinary blower/glue input and full live flight. Evidence: `artifacts/v06-solo-calibration-20260927-182615/`, wrapper `artifacts/v06-solo-calibration-final.log`. Preparation finished with roughly 59 seconds remaining; gear contact was around world time 82.87, result around 89.32 (construction started around 14.02). Shared growth remained 3.00. The fixture uses support measurements to aim; these times are not a human-performance benchmark.
- Rendered/capture review completed for flyby, `?`, `!`, blocked customer mirror, bracing during descent, live touchdown and the outcome poster (`artifacts/v06-{flyby,notice,commit,blocked,brace,touchdown,poster}.png`). Real solo run screenshots also reviewed. No engine/shader errors in the successful capture logs. Successful resolution preserves its support pose; failed helicopters visibly fall after resolution.
- Both actual **`run_ab.ps1 -Variant A` and `-Variant B` entry points** were launched and checked for their requested variant and solo session startup (`artifacts/v06-launch-{A,B}.log`). A PowerShell array-concatenation error discovered in this check was fixed; these launcher checks were intentionally stopped after boot, not counted as match-completion tests.
- CSV export executed successfully against a full two-player log: `artifacts/v06-observer-timeline-example.csv` (602 rows). Structured delivery smoke summary: `artifacts/v06-delivery-summary.json`. Source/script/test/asset hashes: `artifacts/v06-source-sha256.json`.
- This pass did not repeat the historical 600-second v0.5 stress run. No local executable, credential or dependency blocker remains. No human A/B preference or experiential acceptance is claimed.

## Experiment record and human acceptance

Authoritative A/B logs are written automatically to `artifacts/v06-A-*.jsonl` / `v06-B-*.jsonl`, or next to a smoke report. `run_ab.ps1 -Log path.jsonl` chooses a filename. Each row carries UTC, game time, variant, round, event, actor, source/position, detail and the latest ordinary incident/event ID. Logs include phase, helicopter stages, perception/occlusion attempts, reaction transitions, sound novelty, brace start/stop, tool onsets, result, and every active player's position/look/buttons at 2 Hz. Snapshots contain bounded current facts; logs are streamed only by the host.

```powershell
./scripts/export_v06_timeline.ps1 -Log artifacts/v06-B-EXAMPLE.jsonl
```

The CSV exporter preserves timestamps for video alignment. Use `docs/V06_PLAYTEST_RECORD_TEMPLATE.csv` for manual ten-second behavior sampling. Tool counts do not infer intent: task-motivated / novelty / ambiguous coding still requires the group's voice/action context. Replay retains perception, bracing, helicopter, props and material history. No automatic voice/video recording is installed; the participants should use their normal recording setup with consent.

The pre-registered protocol and decision rules in docs **35–36 are unchanged**. Human solo feel/calibration, a familiar four-person group's counterbalanced session, third-exposure behavior, independent causal-story recall and the one-more-round choice remain pending. B is not declared the preferred product direction. The optimized solo fixture finishes preparation early; human waiting/novelty time is therefore a particularly important diagnostic, not a reason to add an unregistered early-call reward. LAN/WAN conditions beyond localhost have not been measured.

## Playtest follow-up: explicit landing failure reasons
B now shows host-measured blockers during contact and retains them with bilingual advice at results/completion. Softness, missing support, uneven/tilted support, fire, loss of contact, customer injury/damage, and insufficient dwell are distinct. Success rules and A remain unchanged. Host landing_support/result logs include measured contacts, height spread, resistance, dwell and customer gates.

Verified: 133 core tests, 18 v0.6 engine checks, A solo smoke, B two-peer smoke with matching final state, four inspected Chinese/English contact/result captures. Evidence: artifacts/landing-reasons-*. Reproduce captures with ./scripts/landing_feedback_review.ps1; launch with ./scripts/run_ab.ps1 -Variant B. The original human recording predates the new diagnostic fields.

## Friend distributable (2026-09-29)
Send artifacts/packages/ProjectHairball-B-Windows-20260929.zip (75,428,112 bytes). Extract all files; run ProjectHairball-B.exe. B is the export default; no command-line options, Godot or installed .NET required. Includes Chinese/English instructions, diagnostic-folder opener and dependency notices.

The exact ZIP passed manifest verification, unpacked standalone menu/gameplay review, bundled-CLR module inspection, 18 exported-engine checks, and a two-process full-duration B network run with matching final state. Source build/core checks also passed (133/133). Evidence: artifacts/package-check-20260929-071248/verification.json. Remote-computer/WAN play remains untested. The build is Windows x64 and uses direct IP networking.

Rebuild: ./scripts/package_b.ps1 -BuildId <new-id>
Verify the resulting archive: ./scripts/verify_b_package.ps1 -Archive <zip-path>
