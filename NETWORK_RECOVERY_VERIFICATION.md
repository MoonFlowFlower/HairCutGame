# Hairball network recovery verification — 2026-09-29

Engineering verification passed within the build boundaries below. Real Canada–China recovery and comfort acceptance remain pending.

## Delivery and scope

Delivery: `artifacts/packages/ProjectHairball-B-Windows-20260929-recovery-r15.zip`, Windows x64, self-contained .NET, protocol 9. SHA256: `7098F5928A92D382A5BAF76694B153A837E276BF0E9F4682CA24A650155645E5`. The previous `20260929-smooth.zip` and intermediate evidence are preserved.

The existing A/B/ordinary game is retained. ENet/ZeroTier remains the network environment. Implemented: bounded 900-byte snapshot/replay chunks with 32 outstanding per peer; application checksum and decode before scene replacement; progress heartbeat/watchdogs; 30-second host-controlled team pause; reserved logical identities and in-memory credentials; reconnect with fresh input generations; explicit leave confirmation, close notice, failure/retry UI. Pause freezes construction, customer, helicopter, tools and physical gameplay while UI/network clocks continue. No Steam, host migration, dedicated server, paid middleware, new content or network-device changes.

## Tests actually completed

| Check | Evidence | Result |
|---|---|---|
| Final build and core | `artifacts/recovery-r15-build.log`, `recovery-r15-core.log` | Zero warnings/errors; 149/149 core tests |
| Engine and A/B regressions | `artifacts/verify-v06-20260929-131920/` | PASS: existing engine suites, A solo/4P, B solo/full 2P/4P/client loss/host loss |
| Ordinary mode 4P, three rounds | `artifacts/smoke-4-ego-20260929-134908-397/` | PASS, final r15 source; order/cancel/pickup/use and shared completion |
| 26 network scenarios | `artifacts/recovery-matrix-20260929-132623/`, `artifacts/recovery-final-coverage.json` | 26/26 PASS; maximum finite-fault recovery 4.7251 seconds |
| Actual old protocol client | `artifacts/recovery-none-20260929-131912-19910/` | r11/protocol-8 client receives explicit version rejection from protocol-9 host |
| Four-player endurance | `artifacts/recovery-blackout-20260929-131902-19900/` | PASS; 14 completed matches, 20 periodic five-second outages, maximum recovery 5.2325 seconds |
| Final bilingual UI logic | `artifacts/recovery-r15-ui-engine.log` | PASS: waiting/failure/retry/continue/leave cancellation, including retry during recovery |
| Active manual retry | `artifacts/recovery-blackout-20260929-135134-20300/` | PASS: deadline unchanged, recovery 2.5383 seconds |
| Final ZIP extraction/run | `artifacts/package-check-20260929-135251/` | PASS: manifest, Chinese/space path, bundled CLR, rendered menu, engine B checks and complete 2P game |
| Motion and replay | `artifacts/recovery-r14-motion-cross.log`, `recovery-r14-motion-relay.log` | PASS: cross-border 4P and relay-stress 2P, full match/replay/state agreement |

Build boundary: the full 26-case matrix, endurance and motion scenarios use the frozen r14 executable. r15 retains that automatic recovery/transport/gameplay implementation and adds the user-triggered Retry button during reconnection/synchronization, its deadline-preserving callback, and a per-peer stale-RTT diagnostic flag. These additions have dedicated r15 UI/manual-retry checks, a fresh core run and independent final-ZIP verification. Do not describe the 30-minute run as an r15 binary run.

## Endurance and fault evidence

The host's monotonic duration was **1801.578 seconds**; clients start after the host and report approximately 1799.7 seconds. The requested fixture was `--seconds 1800`; physics accumulation is recorded separately and is not the duration criterion. Four final world hashes all equal `CDD809A784D0E77E34DECD3C401F07580D8C708DE96310DE3920C2D346FB915F`. Pause invariants passed; four logical players remained. All 60 client recoveries corresponding to the 20 injected outages met the 8-second limit.

There were 24 recovery windows: 22 paused and two during cooldown without another team pause. Four additional progress-stall recoveries occurred beyond the injected outages; all resumed. This is not a claim of zero network interruptions. The run continued through 14 matches without an endless recovery loop or silent frozen state.

Maximum outstanding chunks per player: **32**. Maximum host ENet transport objects: **6**. Host reported peak working set approximately **528 MiB**, clients approximately **388–414 MiB**; sampled host handles 385→390, clients 375→370. `resources.csv` and `resource-summary.json` retain the time series. Memory falls again between matches; diagnostic histories have fixed capacity, and state chunks remain in bounded memory rather than temporary files. These are observations on this development machine, not a friend's hardware benchmark or an unlimited-duration guarantee.

The fault proxy dropped 13,646 datagrams in endurance; maximum proxy queue was 148. The selective chunk test dropped **1,339** datagrams while permitting small packets; recovery took 0.819–2.642 seconds after the filter ended. ACK filtering dropped 10 datagrams. Filters for >1200-byte datagrams and ENet reliable bulk commands matched **zero** packets on the new bulk path; those passing cases are flow checks, not successful injection of a nonexistent packet type. Pure tests additionally exercise chunk loss, lost ACKs, reordering, duplicates, corruption, bounds, invalid credentials, stale attempts, occupied identities and reservation expiry.

## Actual rendered checks

- English failure: `artifacts/recovery-blackout-20260929-134424-20038/recovery-Failed-en.png`.
- Chinese team wait: `artifacts/recovery-chunks-20260929-134658-20050/recovery-Waiting-zh.png`.
- r15 active retry: `artifacts/recovery-blackout-20260929-135134-20300/recovery-Reconnecting-zh.png`.

These images were opened and inspected: legible reason/countdown/buttons, retained scene, no clipped text. Automated UI assertions cover both locales. Rendered/headless synthetic motion metrics do not prove human comfort.

Motion/replay evidence: `artifacts/net-cross-border-new-20260929-135058/` (4P) and `artifacts/net-relay-stress-new-20260929-135252/` (2P). Maximum measured corrections were 4.29 mm and 76.44 mm respectively, with zero hard corrections. Reverse movement samples were 0/8178 and 1/2816. These are synthetic fixture metrics, not human nausea or real-route acceptance. The final exported menu was also opened and inspected at `artifacts/package-check-20260929-135251/menu.png`.

## Failures retained and claim limits

Original incident evidence: `artifacts/disconnect-audit-20260929-logs1/FINDINGS.md`. The 21,544-byte pending exchange and later disconnect support a reliable delivery/ACK-stall hypothesis, but do not establish ZeroTier, MTU, cross-border packet loss or a program defect as the original cause. Machine clocks were not synchronized.

Original-package selective-loss baseline: `artifacts/recovery-baseline-chunks-20260929-113831-18600/` reproduces stale-state/disconnect symptoms, not the real root cause. Intermediate r8 endurance failed to reconnect one peer; r10 completed the state-integrity run but one recovery took **8.064 seconds**, so it failed the strict latency gate. These were not rounded into passes. Later changes add bounded handshake capacity/cleanup, attempt ordering and reset failed-attempt backoff after a successful handshake. The second disconnect in the 8.064-second event remains causally unexplained. Interrupted r12/r13 and early r15 fixture attempts are not completed evidence.

QA-only fixes include monotonic endurance timing, waiting for rendered frames before capture, repeated final-fixture completion messages, and ordinary-mode robot actions driven by replicated milestones. No gameplay assertions, goal thresholds, shop prices or A/B criteria were weakened.

## Reproduce and human acceptance

See `docs/41_NETWORK_RECOVERY_AND_TESTS.md` for commands. Package `Open_Test_Logs.cmd` opens the common user-data directory containing both `logs/` and `playtests/`. Collect both players' network JSONL and engine logs; room/actor/epoch plus local monotonic times aid correlation. Credentials are not logged; no automatic upload occurs.

Both humans must install the same new ZIP and retain ZeroTier for this task. Canada host and China friend must complete at least three rounds and one approximately five-second interruption, checking automatic continuation, retained scene, no silent menu return, aligned paired logs and no obvious camera yank. Earlier real logs had position corrections around 1.2 m; zero large synthetic corrections is not nausea acceptance. **Original root cause remains unknown; real-route recovery and comfort remain human acceptance gates.**
