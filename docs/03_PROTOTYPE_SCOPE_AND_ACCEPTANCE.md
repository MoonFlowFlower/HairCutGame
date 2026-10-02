# 03 — Prototype Scope & Definition of Done v0.3

The prototype is complete when the following acceptance slice is actually playable and verified.

2026-09-27 migration audit: checked boxes denote implementation plus automated/engine evidence, detailed in ../ACCEPTANCE_REPORT.md and ../PROJECT_STATE.md. Human experiential criteria remain open. Network evidence is localhost, not WAN acceptance.

## A. Existing-build migration
- [x] Current repository inspected before major rewrite.
- [x] Reusable v0.2 systems documented in `PROJECT_STATE.md`.
- [x] Obsolete four-independent-station product flow removed or bypassed cleanly.
- [x] No primary gameplay path still requires each player to own a separate customer.

## B. First-person multiplayer
- [x] 1–4 players can join a host-authoritative session.
- [x] Normal gameplay is first-person.
- [x] Players physically occupy the shared room and can crowd/reposition around the central chair.
- [x] One main held world tool at a time.
- [x] Common pickup/drop/tool controls work.

## C. Shared customer/head
- [x] Exactly one active customer is the shared work target during a round.
- [x] All clients observe the same authoritative head-material result.
- [x] At least 24–40 logical low-poly material patches/chunks or an equivalent readable representation.
- [x] Material can be added, removed, forced/deformed, glued/anchored or equivalent.
- [x] Player hair can receive at least two tool effects and is visible to remote players.

## D. Required tool slice
At minimum six tools/effects must be usable in the shared-head scene:
- [x] clippers or safe cutter,
- [x] growth spray,
- [x] leaf blower,
- [x] vacuum transfer cannon,
- [x] glue gun,
- [x] hedge trimmer.

Strongly preferred before declaring complete:
- [x] sniper/puncture tool,
- [x] flamethrower/fire tool.

For each implemented absurd tool:
- [x] at least one legitimate solution use,
- [x] at least one collateral interaction,
- [ ] effect cause is visually readable. (Human acceptance pending; rendered tool feedback inspected.)

## E. Shared round flow
- [x] Customer entrance/setup.
- [x] Three shared goal choices shown.
- [x] 1–4 players can vote.
- [x] One winning goal selected.
- [x] 75-second build timer.
- [x] No individual early-submit/protected-station mechanic in the primary mode.
- [x] At time zero deliberate edits stop.
- [x] Physical validation runs.
- [x] Shared result and shop-money delta shown.

## F. Helipad goal
- [x] Target preview communicates flat landing surface.
- [x] Shape/area measurement can produce a useful approximate score.
- [x] Tiny helicopter validation object approaches/lands or convincingly proxies landing.
- [x] Rotor/downwash applies a gameplay-relevant force or equivalent disturbance.
- [x] Validation success/failure is understandable without reading debug data.

## G. Customer life / panic
- [x] Hidden panic/arousal state exists.
- [x] At least three stimuli modify it (e.g. gunshot, nearby flame, impact/loud tool).
- [x] Customer has at least two small gameplay-relevant reactions (e.g. duck + turn/recoil).
- [x] Reactions have readable anticipation cues where practical.
- [x] Reactions can cause a real miss/collision rather than only playing animation.
- [x] Reactions are rate-limited so customer does not constantly sabotage players.

## H. Player/world physical consequence
- [x] Blower or equivalent can affect player body position/impulse at close range.
- [x] At least one tool can affect a player through their own hair or body.
- [x] Dropped/loose validation props respond physically enough to create readable accidents.

## I. Networking
- [x] Host resolves authoritative tool effects.
- [x] Head state remains acceptably synchronized for host + 1 client.
- [x] Host + 3 clients local smoke test attempted when machine permits.
- [x] No per-hair-cosmetic-transform replication flood.
- [x] Customer panic/reaction trigger is authoritative.
- [x] Shared round/goal/result remains synchronized.

## J. Tests and tooling
- [x] `dotnet build` or equivalent C# build path documented.
- [x] Automated logic tests exist for key hair/effect/round logic where practical.
- [x] Repeatable solo launch command/script.
- [x] Repeatable host + client launch script.
- [x] Preferably local four-instance launcher.
- [x] Actual executed tests/results recorded in `PROJECT_STATE.md`.

## K. Human playtest criteria
The next human playtest should be able to answer:
- [ ] Did players naturally crowd, negotiate, and interrupt each other around one head?
- [ ] Did at least one accident have a clear causal chain?
- [ ] Did someone naturally say the equivalent of “don't move”, “who did that”, or “I was trying to fix it”?
- [ ] Were customer reactions funny more often than annoying?
- [ ] Was the helipad validator tense/funny?
- [ ] Did dangerous/faster methods tempt players despite risk?

## Stretch acceptance after core is stable
- [x] Bird Nest goal.
- [x] Llama customer with spit reaction.
- [x] Highlight/replay clip.
- [x] Shared wallet across several customer jobs.


